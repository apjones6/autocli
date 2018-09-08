using System;

namespace AutoCli.Resolvers
{
	/// <summary>
	/// An implementation of <see cref="IResolver"/> which wraps a provided function.
	/// </summary>
	public class Resolver : IResolver
	{
		private readonly Func<Type, object> func;

		/// <summary>
		/// Initializes a new instance of the <see cref="Resolver"/> class with the provided function.
		/// </summary>
		/// <param name="func">The resolver function.</param>
		public Resolver(Func<Type, object> func)
		{
			this.func = func ?? throw new ArgumentNullException(nameof(func));
		}

		/// <inheritdoc />
		public virtual object Resolve(Type serviceType)
		{
			return func(serviceType);
		}
	}
}
