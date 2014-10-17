using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Croc.Tools.ConfluenceConverter
{
	public class ConfluenceConverter
	{
		private static readonly Regex s_reCode;
		private static readonly Regex s_reNoFormat;
		private static readonly Regex s_reImage;
		private static readonly Regex s_reToc;
		private static readonly Regex s_reChildren;

		class Transform
		{
			public Transform(string name, Regex regex, MatchEvaluator replaceAction)
			{
				m_name = name;
				m_regex = regex;
				m_replaceAction = replaceAction;
			}

			public Transform(string name, Regex regex, string replaceText)
			{
				m_name = name;
				m_regex = regex;
				m_replaceText = replaceText;
			}

			private readonly string m_name;
			private readonly Regex m_regex;
			private readonly MatchEvaluator m_replaceAction;
			private readonly string m_replaceText;

			public string Process(string input)
			{
				if (m_replaceAction != null)
					return m_regex.Replace(input, m_replaceAction);
				return m_regex.Replace(input, m_replaceText);
			}
		}

		private readonly List<Transform> m_steps;
		private readonly ConfluenceConverterOptions m_options;
		private readonly ILinkResolver m_linkResolver;

		static ConfluenceConverter()
		{
			s_reCode = new Regex(@"\{code(\:([^\}]+?))?\}\s*([.\s\S]*?)\s*\{code\}", RegexOptions.Multiline);
			s_reNoFormat = new Regex(@"\{noformat\}\s*([.\s\S]*?)\s*\{noformat\}", RegexOptions.Multiline);
			s_reImage = new Regex(@"!(\w[^!]+\w)!");
			s_reToc = new Regex(@"\{toc\}");
			s_reChildren = new Regex(@"\{children\}");
			/*
						s_reHeading = new Regex(@"h([\d]+).\s");
						s_reParagraph = new Regex(@"\s\\\\\s", RegexOptions.Multiline);
						s_reOrderedList = new Regex(@"^[#]+[ \t]", RegexOptions.Multiline);
						s_reUnorderedList = new Regex(@"^[*+-]+\s");
						s_reBold = new Regex(@"\*([^*\s]+)\*");
						s_reItalic = new Regex(@"_([^_\s]+)_");
						s_reInlineCode = new Regex(@"\{\{([^}]+)\}\}");
						s_reLink = new Regex(@"\[(([^|]+)\|)?([^\]]+)\]");
						s_reNotes = new Regex(@"\{note(\:([\w]+))?\}\s*([.\s\S]*?)\s*\{note\}");
			*/
		}

		public ConfluenceConverter(ConfluenceConverterOptions options, ILinkResolver linkResolver)
		{
			m_options = options;
			m_linkResolver = linkResolver;
			m_steps = new List<Transform>
			{
				// Paragraphs
				new Transform("Paragraph", new Regex(@"\s\\\\\s", RegexOptions.Multiline), Environment.NewLine+Environment.NewLine),

				// Horizontal Rules
				new Transform("Horizontal Rule", new Regex("^----+$"), "---"),

				// Lists
				new Transform("OrderedList", new Regex(@"^[#]+[ \t]", RegexOptions.Multiline), "1. "),
				// TODO: 2nd level and so on
				// if we add "-" as item symbol then it'll catch horizontal rule "---" :(
				new Transform("UnorderedList", new Regex(@"^([*+-]+)[ \t]", RegexOptions.Multiline), match =>
				{
					string term = match.Groups[1].Value;
					var builder = new StringBuilder();
					for (int i = 0; i < term.Length-1; i++)
					{
						builder.Append("  ");
					}
					builder.Append("* ");
					return builder.ToString();
				}),

				// Headings
				// TODO: streamline cases with missed headings like "h3. sec1 // h6. sec6"
				new Transform("Headings", new Regex(@"h([\d]+).\s"), match =>
				{
					string level = match.Groups[1].Value;
					int nLevel;
					if (Int32.TryParse(level, out nLevel))
					{
						var builder = new StringBuilder();
						for (int i = 0; i < nLevel; i++)
						{
							builder.Append("#");
						}
						if (builder.Length > 0)
						{
							builder.Append(" ");
							return builder.ToString();
						}
					}
					return match.Value;
				}),

				// Character styles
				new Transform("Character styles", new Regex(@"\s\*([^*\s]+?)\*\s"), match =>
				{
					string term = match.Groups[1].Value;
					return " **" + term + "** ";
				}),
				// TODO: italic (_text_), strikethrough (-text-), underlined (+text+), superscript (^text^), subscript (~text~)
				// TODO: проблемы с обработкой имен файлов, e.g. "!ajax_screenshot_1.png!"
				//markup = s_reItalic.Replace(markup, match =>
				//{
				//	string term = match.Groups[1].Value;
				//	return "*" + term + "*";
				//});

				// inline code
				new Transform("Inline code", new Regex(@"\{\{([^}]+)\}\}"), match =>
				{
					string term = match.Groups[1].Value;
					return "`" + term + "`";
				}),

				// links
				new Transform("Link", new Regex(@"\[(([^|\]]+?)\|)?([^\]]+?)\]"), match =>
				{
					string link = match.Groups[3].Value;
					string alias = match.Groups[2].Value;
					if (string.IsNullOrEmpty(alias) && link.StartsWith("http"))
					{
						// it's just a link
						return link;
					}
					string href = link;
					if (!link.StartsWith("http"))
					{
						href = linkResolver.Resolve(link);
					}
					if (string.IsNullOrEmpty(alias))
					{
						alias = link;
					}
					return "[" + alias + "](" + href + ")";
				}),

				// TODO: blocks inside table!
				// note/warning/info/tip blocks
				new Transform("note", new Regex(@"\{note\}\s*([.\s\S]*?)\s*\{note\}"), match =>
				{
					string content = match.Groups[1].Value;
					return Environment.NewLine + "> " + content + Environment.NewLine;
				}),
				new Transform("warning", new Regex(@"\{warning\}\s*([.\s\S]*?)\s*\{warning\}"), match =>
				{
					string content = match.Groups[1].Value;
					return Environment.NewLine + "> " + content + Environment.NewLine;
				}),
				new Transform("info", new Regex(@"\{info\}\s*([.\s\S]*?)\s*\{info\}"), match =>
				{
					string content = match.Groups[1].Value;
					return Environment.NewLine + "> " + content + Environment.NewLine;
				}),
				new Transform("tip", new Regex(@"\{tip\}\s*([.\s\S]*?)\s*\{tip\}"), match =>
				{
					string content = match.Groups[1].Value;
					return Environment.NewLine + "> " + content + Environment.NewLine;
				}),
				new Transform("quote", new Regex(@"\{quote\}\s*([.\s\S]*?)\s*\{quote\}"), match =>
				{
					string content = match.Groups[1].Value;
					return Environment.NewLine + "> " + content + Environment.NewLine;
				}),

				// {anchor:anchorname}
				new Transform("{anchor}", new Regex(@"\{anchor:([^\}]+?)\}"), match =>
				{
					string name = match.Groups[1].Value;
					return "<a id='" + name + "'></a>";
				}),

				new Transform("{color}", new Regex(@"\{color:([^\}]+?)\}(.+?)\{color\}"), match =>
				{
					string color = match.Groups[1].Value;
					string text = wiki2Markdown(match.Groups[2].Value);
					return string.Format("<span style='color: {0}'>{1}</span>", color, text);
				})
			};
			
			// TODO: {color:red}{color}
			// TODO: {toc}
		}

		private string wiki2Markdown(string markup)
		{
			var blocks = new Dictionary<string, string>();
			// code:
			markup = s_reCode.Replace(markup, match =>
			{
				string code = match.Groups[3].Value;
				string lang = match.Groups[2].Value;
				var builder = new StringBuilder();
				builder.AppendLine();
				builder.Append("```");
				if (!String.IsNullOrWhiteSpace(lang))
				{
					builder.Append(lang);
				}
				builder.AppendLine();
				builder.Append(code);
				builder.AppendLine();
				builder.Append("```");

				var tokenId = "%" + Guid.NewGuid().ToString("N") + "%";
				blocks[tokenId] = builder.ToString();
				return tokenId;
			});
			// noformat
			markup = s_reNoFormat.Replace(markup, match =>
			{
				string block = match.Groups[1].Value;
				var builder = new StringBuilder();
				builder.AppendLine();
				builder.Append("```");
				builder.AppendLine();
				builder.Append(block);
				builder.AppendLine();
				builder.Append("```");

				var tokenId = "%" + Guid.NewGuid().ToString("N") + "%";
				blocks[tokenId] = builder.ToString();
				return tokenId;
			});

			// execute all simple transformations
			foreach (var transform in m_steps)
			{
				markup = transform.Process(markup);
			}

			// images
			markup = s_reImage.Replace(markup, match =>
			{
				string file = match.Groups[1].Value;
				// TODO: resolve file path ("{{ site.attachmentsDir }}")
				return "![" + file + "](" + m_options.AttachmentsDir + "/" + file + ")";
			});

			// put back code/noformat blocks
			foreach (var pair in blocks)
			{
				markup = markup.Replace(pair.Key, pair.Value);
			}

			// TODO: toc/children
			// new Transform("{toc}", new Regex(@"\{toc\}"), String.Empty),
			//	new Transform("{children}", new Regex(@"\{children\}"), String.Empty),
			if (s_reToc.IsMatch(markup))
			{
				// {toc}
				// {toc:maxLevel=4}
				// {toc:minLevel=2|maxLevel=4}
				// TODO
			}

			// tables
			markup = convertTables(markup);

			// encode "{{" for HB-safety
			if (m_options.HandlebarsComplience)
			{
				markup = markup.Replace("{{", "\\{{");
			}

			// TODO: HARDCODED FIX
			markup = markup.Replace("`$CLASS$`", "` $CLASS$ `");

			return markup;
		}

		private string convertTables(string markup)
		{
			var reTableHeader = new Regex(@"\|\|(\s*([^|\n\r]*)\s*\|\|)+", RegexOptions.Multiline);
			markup = reTableHeader.Replace(markup, match =>
			{
				var columns = match.Groups[2].Captures.Cast<Capture>().Select(cap => cap.Value).ToList();
				//string headerRow = String.Join(" | ", columns) + Environment.NewLine;
				var builder = new StringBuilder(Environment.NewLine);
				builder.Append("| ");
				builder.Append(String.Join(" | ", columns));
				builder.AppendLine(" |");
				for (int i = 0; i < columns.Count; i++)
				{
					builder.Append("|");
					builder.AppendFormat("-");
				}
				builder.Append(" |");

				//return match.Value;
				return builder.ToString();
			});
			/* in wiki tables are:
||heading 1||heading 2||heading 3||
|col A1|col A2|col A3|
|col B1|col B2|col B3|
			 OR w/o header row:
|col A1|col A2|col A3|
|col B1|col B2|col B3|
			 */

			/* in Markdown tables are:
(from https://help.github.com/articles/github-flavored-markdown/#tables)
First Header  | Second Header
------------- | -------------
Content Cell  | Content Cell
Content Cell  | Content Cell
			 OR:
| First Header  | Second Header |
| ------------- | ------------- |
| Content Cell  | Content Cell  |
| Content Cell  | Content Cell  |
			 Cells can include inline MD (but not block MD):
| Name | Description          |
| ------------- | ----------- |
| Help      | ~~Display the~~ help window.|
| Close     | _Closes_ a window     |
			 * 
			 */
			return markup;
		}

		public void ToMarkdown(Page page)
		{
			if (!string.IsNullOrWhiteSpace(page.ContentMd))
				return;

			// convert wiki to markdown
			string markdown = wiki2Markdown(page.ContentWiki);

			// if a page contains nothing or "{children}" then generate its child list recursively
			if (String.IsNullOrWhiteSpace(markdown.Trim()) || markdown.IndexOf("{children}", StringComparison.InvariantCultureIgnoreCase) > -1)
			{
				if (String.IsNullOrWhiteSpace(markdown.Trim()))
				{
					markdown = "{children}";
				}
				var builder = new StringBuilder();
				page.VisitChildren((p, path) =>
				{
					for (int i = 0; i < path.Count-1; i++)
					{
						builder.Append("  ");
					}
					builder.AppendFormat("* [{0}]({1}){2}", p.Title, m_linkResolver.Resolve(p.Title), Environment.NewLine);
				});
				markdown = s_reChildren.Replace(markdown, builder.ToString());
			}

			page.ContentMd = markdown;
		}
	}
	/*
	 * Headings:
	 * h1. -> #
	 * h2. -> ##
	 * 
	 * Paragraphs:
	 * \\ -> Empty line
	 * 
	 * Character styles:
	 * *xxx* (bold) -> **xxx**
	 * _xxx_ (italic) -> *xxx*
	 * 
	 * lists:
	 * *, * -> the same
	 * 
	 * inline code characters:
	 * {{char}} -> `char`
	 * 
	 * code:
	 * {code}
	 * {/code}
	 * => 
	 * ```
	 * ```
	 * {code:js}
	 * {code}
	 * =>
	 * ```js
	 * ```
	 * 
	 * links:
	 * [page] => ?
	 * [alias|page] => ?
	 * [http://xxx] => http://xxx
	 * [alias|http://xxx] => [alias](http://example.net/)
	 * 
	 * images:
	 * !image.png! => ![Alt text](/path/to/image.jpg) 
	 * 
	 * tables:
	 * ||column||
	 * | value | 
	 * =>
	 */
}
