using System;
using System.Text;

namespace Croc.Tools.ConfluenceConverter
{
	public interface ISpaceProcessor
	{
		void Process(Space space);
	}

	public class HierarchyProcessor : ISpaceProcessor
	{
		public void Process(Space space)
		{
			foreach (var root in space.RootPage.Children)
			{
				if (root.Children.Count > 0)
				{
					// a 1st level topic has children - it's a section
					root.Section = root.Title;
					foreach (var child in root.Children)
					{
						child.Section = root.Title;
					}
				}
				else
				{
					// 1st level topic has no children - is'a a page
					root.Section = "root";
				}
			}
			space.RootPage.Ignore = true;
		}
	}
}