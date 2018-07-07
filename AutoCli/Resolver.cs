using System;

namespace AutoCli
{
	public class Resolver
	{
		private readonly Func<Type, object> func;

		public Resolver(Func<Type, object> func)
		{
			this.func = func ?? throw new ArgumentNullException(nameof(func));
		}

		public virtual object Resolve(Type serviceType)
		{
			return func(serviceType);
		}
	}
}
