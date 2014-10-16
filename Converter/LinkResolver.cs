using System.Linq;

namespace Croc.Tools.ConfluenceConverter
{
	public interface ILinkResolver
	{
		string Resolve(string link);
	}

	public class LinkResolver : ILinkResolver
	{
		private readonly Space m_space;

		public LinkResolver(Space space)
		{
			m_space = space;
		}

		public string Resolve(string link)
		{
			// it's a wiki link:
			//	wiki-extenral: [xfwajax:Release 0.15] -> other space "xfwajax"
			//	wiki-global: [Core] or [see also|Core] -> page "Core" in the same space
			//	wiki-global+anchor: [Core#Section 1] or [see also|Core#Section 1] -> page "Core" + heading "Section 1" in the same space
			//  wiki-local: [#Section 2] or [see below|#Section 2] ->  heading or anchor "Section 2" at the same page

			if (link.IndexOf(":") > -1)
			{
				// cross-space link a:b
				int idx = link.IndexOf(":");
				string spaceName = link.Substring(0, idx);
				link = link.Substring(idx + 1);
				// TODO: if link referes to a page with not-English title: http://wiki.rnd.croc.ru/pages/viewpage.action?pageId=31424726
				link = "http://wiki.rnd.croc.ru/display/" + spaceName + "/" + link.Replace(" ", "+");
			}
			else if (link.StartsWith("#"))
			{
				// TODO: in-page link
			}
			else
			{
				// space-scoped link: as link equals to a page's title we need to change it onto page's file name
				var hashIdx = link.IndexOf("#");
				string hashLink = null;
				if (hashIdx > -1)
				{
					hashLink = link.Substring(hashIdx + 1);
					link = link.Substring(0, hashIdx);
				}
				var page = m_space.AllPages.FirstOrDefault(p => p.Title == link);
				if (page != null)
				{
					link = page.Name;
				}
				link = link + ".html";
				if (!string.IsNullOrEmpty(hashLink))
				{
					link = link + "#" + hashLink;
				}
			}
			return link;
		}
	}
}