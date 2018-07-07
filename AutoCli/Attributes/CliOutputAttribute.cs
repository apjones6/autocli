using System;
using System.Collections.Generic;
using System.Text;

namespace AutoCli.Attributes
{
	[AttributeUsage(AttributeTargets.Property)]
    public class CliOutputAttribute : Attribute
    {
		public CliOutputAttribute()
		{
		}

		public bool Key { get; set; }

		public int Order { get; set; }
    }
}
