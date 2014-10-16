using System;
using System.Collections.Generic;

namespace Croc.Tools.ConfluenceConverter
{
	public class Space
	{
		public Page RootPage { get; set; }

		public IList<Page> AllPages { get; private set; }
		public IDictionary<String, String> Labels { get; private set; }

		public Space()
		{
			AllPages = new List<Page>();
			Labels = new Dictionary<string, string>();
		}

		public void AddLabel(string id, string name)
		{
			Labels[id] = name;
		}

		public void Visit(Action<Page, Stack<Page>> visitor)
		{
			var rootPath = new Stack<Page>();
			visit(RootPage, visitor, rootPath);
		}

		private void visit(Page page, Action<Page, Stack<Page>> visitor, Stack<Page> rootPath, bool onlyChildren = false)
		{
			if (!onlyChildren)
			{
				visitor(page, rootPath);
			}
			rootPath.Push(page);
			foreach (var child in page.Children)
			{
				visit(child, visitor, rootPath);
			}
			rootPath.Pop();
		}

		public void Visit(Page startPage, Action<Page, Stack<Page>> visitor, bool onlyChildren = false)
		{
			var rootPath = new Stack<Page>();
			visit(startPage, visitor, rootPath, onlyChildren);
		}
	}

	public class Page 
	{
		private List<String> m_labelRefs;

		public Page()
		{
			Children = new List<Page>();
			Attachments = new List<Attachment>();
			Labels = new List<String>();
			m_labelRefs = new List<String>();
		}
		/// <summary>
		/// Internal numeric Id from Confluence. Isn't used in Markdown.
		/// </summary>
		public String Id;
		/// <summary>
		/// Page title
		/// </summary>
		public String Title;
		/// <summary>
		/// Name - it's used for file name
		/// </summary>
		public String Name;
		public String Section;
		public Page Parent;
		public List<Page> Children;
		public List<String> Labels;
		public Int32 Position;

		public String ContentWiki;
		public String ContentMd;
		public IList<Attachment> Attachments;
		public Boolean Ignore;
		public Space Space;

		public void VisitChildren(Action<Page, Stack<Page>> visitor)
		{
			Space.Visit(this, visitor, onlyChildren:true);
		}

		internal void AddLabelRef(string labelRef)
		{
			m_labelRefs.Add(labelRef);
		}

		internal void InitLabels(IDictionary<string, string> labels)
		{
			foreach (string labelId in m_labelRefs)
			{
				string name;
				if (labels.TryGetValue(labelId, out name))
				{
					Labels.Add(name);
				}
			}
			m_labelRefs = null;
		}
	}

	public class Attachment 
	{
		public string Id;
		public string FileName;
		public string ContentType;
		public Page Page;
		public String Version;
	}

	public enum WikiObjectClass
	{
		Page,
		Space,
		BodyContent,
		OutgoingLink,
		Attachment,
		Labelling,
		Label,
		//SpacePermission,
		Comment
	}
}