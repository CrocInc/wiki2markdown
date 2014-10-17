using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Security.Policy;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Croc.Tools.ConfluenceConverter
{
	public class ConfluenceConverterOptions
	{
		public String Extension { get; set; }
		public Boolean HandlebarsComplience { get; set; }
		public String AttachmentsDir { get; set; }
		public Boolean Print { get; set; }
		public string FrontMetaTemplate { get; set; }
		public Boolean Slugify { get; set; }
	}

	class Program
	{
		private static void terminate(string msg = null)
		{
			Console.WriteLine("Confluence wiki markup to markdown converter.\nUSAGE: conf2md.exe <xml-file> <output dir path>");
			if (!String.IsNullOrWhiteSpace(msg))
			{
				var c = Console.ForegroundColor;
				Console.ForegroundColor = ConsoleColor.Red;
				Console.WriteLine(msg);
				Console.ForegroundColor = c;
			}
			Environment.Exit(-1);			
		}

		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				terminate();
			}
			// Input:
			string input = args[0];
			string inputFilePath = null;
			if (File.Exists(input))
			{
				inputFilePath = input;
			} 
			else if (Directory.Exists(input))
			{
				inputFilePath = Path.Combine(input, "entities.xml");
			}
			else
			{
				// a non existing file or dir
				terminate("A non-existing file or directory was specified: " + input);
			}
			// Output:
			string outDir = args[1];
			// Options:
			var options = new ConfluenceConverterOptions
			{
				AttachmentsDir = "attachments",
				Print = true
			};
			for (int i = 0; i < args.Length; i++)
			{
				var arg = args[i];
				if (arg.StartsWith("-"))
				{
					// options:
					if (arg.StartsWith("-ext:"))
					{
						options.Extension = arg.Substring(5);
					}
					if (arg.StartsWith("-handlebars"))
					{
						options.HandlebarsComplience = true;
					}
					if (arg.StartsWith("-attachments:"))
					{
						options.AttachmentsDir = "attachments";
					}
					if (arg.StartsWith("-noprint"))
					{
						options.Print = false;
					}
					if (arg.StartsWith("-frontmeta:"))
					{
						string frontMetaTemplateFile = arg.Substring("-frontmeta:".Length);
						if (!File.Exists(frontMetaTemplateFile))
						{
							terminate("Front-meta template does not exists: " + frontMetaTemplateFile);
						}
						options.FrontMetaTemplate = File.ReadAllText(frontMetaTemplateFile);
					}
					if (arg.StartsWith("-slugify"))
					{
						options.Slugify = true;
					}
					// Sections: Base,Core,Getting Started, Application,UI-UX,AppServices,Server,DevTools
				}
			}
			var dirInfo = Directory.CreateDirectory(outDir);

			// parse 
			XDocument xDoc = XDocument.Load(inputFilePath);
			var reader = new ConfluenceReader();
			Space space = reader.Read(xDoc);

			// extract attachments
			string attachmentsDir = Path.Combine(dirInfo.FullName, "attachments");
			Directory.CreateDirectory(attachmentsDir);
			string wikiAttachmentsDir = Path.Combine(Path.GetDirectoryName(inputFilePath), "attachments");
			reader.ExtractAttachments(space, wikiAttachmentsDir , attachmentsDir);

			if (options.Print)
			{
				DumpWiki(space);
			}

			// convert into markdown
			string extension = options.Extension ?? ".md";
			var processors = new List<IPageProcessor>
			{
				new TemplatePageProcessor(options)
			};
			var linkResolver = new LinkResolver(space);
			var converter = new ConfluenceConverter(options, linkResolver);
			new HierarchyProcessor().Process(space);
			space.Visit((p, rootPath) =>
			{
				// convert into markdown
				converter.ToMarkdown(p);
				// additional processing
				foreach (var processor in processors)
				{
					processor.Process(p);
				}

				// save output
				if (!p.Ignore)
				{
					var outFilePath = Path.Combine(dirInfo.FullName, p.Name + extension);
					using (StreamWriter sw = File.CreateText(outFilePath))
					{
						sw.Write(p.ContentMd);
					}
				}
			});
		}

		static void DumpWiki(Space space)
		{
			var builder = new StringBuilder();
			space.Visit((p, rootPath) =>
			{
				for (int i = 0; i < rootPath.Count; i++)
				{
					builder.Append("\t");
				}
				builder.Append(p.Title);
				builder.AppendLine();
			});
			Console.WriteLine(builder.ToString());
		}
	}
}
