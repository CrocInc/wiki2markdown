using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace Croc.Tools.ConfluenceConverter
{
	public class WikiObject
	{
		public WikiObject()
		{
			Attributes = new Dictionary<string, string>();
		}

		public string Id;
		public WikiObjectClass? Class;
		public IDictionary<String,String> Attributes;

		public string GetAttributeValue(string name)
		{
			string value;
			if (Attributes.TryGetValue(name, out value))
			{
				return value;
			}
			return null;
		}
	}

	public class ConfluenceParser
	{
		public IList<WikiObject> Parse(XDocument xDoc)
		{
			if (xDoc.Root == null)
				return null;

			var objects = new List<WikiObject>();
			foreach (var xObj in xDoc.Root.Elements("object"))
			{
				var wikiObj = new WikiObject();				
				var xClass = xObj.Attribute("class");
				if (xClass == null)
					continue;
				WikiObjectClass wikiClass;
				if (Enum.TryParse(xClass.Value, true, out wikiClass))
				{
					wikiObj.Class = wikiClass;
				}
				else
				{
					continue;
				}

				foreach (var xProperty in xObj.Elements())
				{
					var xName = xProperty.Attribute("name");
					if (xName  == null)
						continue;
					string propName = xName.Value;
					string propValue;
					if (xProperty.Element("id") != null)
					{
						propValue = xProperty.Element("id").Value;
					}
					else
					{
						propValue = xProperty.Value;
					}
					

					switch (propName)
					{
						case "id":
							wikiObj.Id = propValue;
							break;
						default:
							wikiObj.Attributes.Add(propName, propValue);
							break;
					}
				}
				if (wikiObj.Id == null || wikiObj.Class == null)
					continue;
				objects.Add(wikiObj);
			}
			return objects;
		}
	}

	public class ConfluenceReader
	{
		public Space Read(XDocument xDoc)
		{
			var parser = new ConfluenceParser();
			var wikiObjects = parser.Parse(xDoc);
			return Read(wikiObjects);
		}

		public Space Read(IList<WikiObject> wikiObjects)
		{
			var space = new Space();
			// all space pages: id -> Page
			var pagesById = new Dictionary<string, Page>();
			// all space labels: id -> name
			var labels = new Dictionary<string, string>();
			foreach (WikiObject wikiObj in wikiObjects)
			{
				switch (wikiObj.Class)
				{
					case WikiObjectClass.Page:
						if (wikiObj.Attributes.ContainsKey("originalVersion"))
							continue;
						Page page = new Page();
						page.Id = wikiObj.Id;
						pagesById[page.Id] = page;
						break;
				}
			}
			foreach (WikiObject wikiObj in wikiObjects)
			{
				Page page;
				string pageId;

				switch (wikiObj.Class)
				{
					case WikiObjectClass.Page:
						if (pagesById.TryGetValue(wikiObj.Id, out page))
						{
							initPage(page, wikiObj, space, pagesById);
						}
						break;
					case WikiObjectClass.Attachment:
						if (wikiObj.Attributes.TryGetValue("content", out pageId))
						{
							if (pagesById.TryGetValue(pageId, out page))
							{
								if (wikiObj.Attributes.ContainsKey("originalVersion"))
									continue;

								var attach = new Attachment();
								page.Attachments.Add(attach);
								attach.Id = wikiObj.Id;
								attach.Page = page;
								attach.FileName = wikiObj.GetAttributeValue("fileName");
								attach.ContentType = wikiObj.GetAttributeValue("contentType");
								attach.Version = wikiObj.GetAttributeValue("attachmentVersion");
							}
						}
						break;
					case WikiObjectClass.BodyContent:
						if (wikiObj.Attributes.TryGetValue("content", out pageId))
						{
							if (pagesById.TryGetValue(pageId, out page))
							{
								page.ContentWiki = wikiObj.Attributes["body"];
							}
						}
						break;
					case WikiObjectClass.Space:
						// name:string
						// key:string
						// description:Ref-SpaceDescription
						// homePage:Ref-Page
						// permissions
						break;
					case WikiObjectClass.Labelling:
						// label -> Label.Id
						// content -> Page.Id
						if (wikiObj.Attributes.TryGetValue("content", out pageId))
						{
							if (pagesById.TryGetValue(pageId, out page))
							{
								page.AddLabelRef(wikiObj.GetAttributeValue("label"));
							}
						}
						break;
					case WikiObjectClass.Label:
						labels[wikiObj.Id] = wikiObj.GetAttributeValue("name");
						break;
				}
			}
			initHierrachy(space, labels);
			return space;
		}

		void initPage(Page page, WikiObject wikiObj, Space space, IDictionary<String, Page> pagesById)
		{
			foreach (var pair in wikiObj.Attributes)
			{
				var propName = pair.Key;
				var propValue = pair.Value;
				switch (propName)
				{
					case "title":
						page.Title = propValue;
						page.Name = page.Title.Replace(" ", "-");
						page.Name = page.Name.Replace(".", "_");
						page.Name = page.Name.ToLowerInvariant();
						break;
					case "parent":
						Page page1;
						if (pagesById.TryGetValue(propValue, out page1))
						{
							page.Parent = page1;
							page1.Children.Add(page);
						}
						break;
					case "position":
						int position;
						if (Int32.TryParse(propValue ?? "0", out position))
						{
							page.Position = position;
						}
						break;
					//case "bodyContents":
					//case "originalVersion":
					// originalVersion
					// contentStatus==current
					// version
					// historicalVersions
					// creatorName
					// creationDate
				}
			}
			space.AllPages.Add(page);
			page.Space = space;
		}

		private void initHierrachy(Space space, Dictionary<string, string> labels)
		{
			// Set up space root page
			foreach (Page page in space.AllPages)
			{
				if (page.Parent == null)
				{
					// it's the single space root (usualy "Home")
					space.RootPage = page;

					break;
				}
			}
			space.Visit((p, rootPath) =>
			{
				// Sort children by position
				if (p.Children.Count > 0)
				{
					p.Children.Sort((x, y) => x.Position - y.Position);
				}

				// Labels
				p.InitLabels(labels);
			});
		}

		public void ExtractAttachments(Space space, string intputDir, string outputDir)
		{
			space.Visit((p, path) =>
			{
				string pageId = p.Id;
				foreach (var attachment in p.Attachments)
				{
					string inputPath = Path.Combine(intputDir, pageId, attachment.Id, attachment.Version);
					File.Copy(inputPath, Path.Combine(outputDir, attachment.FileName), true);
				}
			});

		}
	}
}
