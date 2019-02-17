using System.Collections.Generic;

namespace AutoCli
{
	/// <summary>
	/// Describes formatting content to be written to the console.
	/// </summary>
	public class ConsoleContent
	{
		/// <summary>
		/// A separator object, used to allow the console serializer to format output better.
		/// </summary>
		public static readonly object SEPARATOR = new object();

		private readonly List<object> contents;

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsoleContent"/> class.
		/// </summary>
		public ConsoleContent()
		{
			contents = new List<object>();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="ConsoleContent"/> class.
		/// </summary>
		/// <param name="content">The initial content.</param>
		public ConsoleContent(object content)
		{
			contents = new List<object> { content };
		}

		/// <summary>
		/// Gets the contents to be written.
		/// </summary>
		public IEnumerable<object> Contents => contents;

		/// <summary>
		/// Adds the provided object to this <see cref="ConsoleContent"/>. If the object
		/// is another instance of <see cref="ConsoleContent"/> the items are merged.
		/// </summary>
		/// <param name="content">The content to add.</param>
		public void Add(object content)
		{
			if (content is ConsoleContent consoleContent)
			{
				contents.AddRange(consoleContent.contents);
			}
			else
			{
				contents.Add(content);
			}
		}
	}
}
