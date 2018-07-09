using System;

namespace AutoCli
{
	/// <summary>
	/// Describes the <see cref="Cli"/> resolver, used to instantiate service instances to invoke.
	/// </summary>
	public interface IResolver
	{
		/// <summary>
		/// Returns an instance of the specified service type.
		/// </summary>
		/// <param name="serviceType">The service type.</param>
		/// <returns>
		/// An instance of the type.
		/// </returns>
		object Resolve(Type serviceType);
	}
}
