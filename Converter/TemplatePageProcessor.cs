using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;

namespace Croc.Tools.ConfluenceConverter
{
	public interface IPageProcessor
	{
		void Process(Page page);
	}

	/// <summary>
	/// Enrich page's markdown markup using a template (kind of YAML)
	/// </summary>
	public class TemplatePageProcessor : IPageProcessor
	{
		private readonly ConfluenceConverterOptions m_options;
		// search for '{something}'
		private readonly Regex m_regex = new Regex(@"\{([^}]+?)\}", RegexOptions.Compiled);

		public TemplatePageProcessor(ConfluenceConverterOptions options)
		{
			m_options = options;
		}

		public void Process(Page page)
		{
			/*if (String.IsNullOrWhiteSpace(page.ContentWiki.Trim()) || page.ContentWiki.Trim() == "{children}")
			{
				page.IsEmpty = true;
			}*/

			string template = m_options.FrontMetaTemplate;
			if (String.IsNullOrWhiteSpace(template))
			{
				return;
			}
			// Add YAML Front matter:
			/*
---
title: Structure
area: Docs
section: root
---
			 */
			var metadata = m_regex.Replace(template, match =>
			{
				var term = match.Groups[1].Value;
				switch (term.ToLowerInvariant())
				{
					case "id":
						return page.Id;
					case "title":
						return page.Title;
					case "name":
						return page.Name;
					case "section":
						return page.Section;
					case "parent":
					case "parent.name":
						return page.Parent != null ? page.Parent.Name : "";
					case "parent.title":
						return page.Parent != null ? page.Parent.Title : "";
					case "position":
						return page.Position.ToString(CultureInfo.InvariantCulture);
					case "children":
						var children = String.Join(Environment.NewLine, page.Children.Select(p => "  - " + p.Name));
						return !string.IsNullOrWhiteSpace(children) ? Environment.NewLine + children : String.Empty;
					case "tags":
					case "labels":
						string tags = String.Join(Environment.NewLine, page.Labels.Select(l => "  - " + l));
						return !string.IsNullOrWhiteSpace(tags) ? Environment.NewLine + tags : String.Empty;
					case "isroot":
						return (page.Parent == null || page.Parent.Parent == null) ? "true" : "false";
				}
				return String.Empty;
			});
			if (!String.IsNullOrWhiteSpace(metadata))
			{
				page.ContentMd = metadata + Environment.NewLine + page.ContentMd;
			}

			/*
						var builder = new StringBuilder();
						builder.AppendLine("---");
						builder.AppendLine("title: " + page.Title);
						builder.AppendLine("area: Docs");
						if (page.Parent != null)
						{
							builder.Append("section: ");
							if (page.Parent.Parent == null)
							{
								builder.AppendLine("root");
							}
							else 
							{
								builder.AppendLine(page.Parent.Title);
							}
						}
						builder.AppendLine("---");
						builder.Append(page.ContentMd);

						return builder.ToString();
			*/
		}
	}
}
