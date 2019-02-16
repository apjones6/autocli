# AutoCli

Expose your services as a CLI (command line interface). The goal is with minimal effort (adding
attributes to interfaces and classes) a fully-functioning CLI with informative messages and errors
can be produced.

## Usage

This library is designed with the intention that you can create a simple console application, decorate your service interfaces, and let AutoCli to the rest of the work. With this in mind, your `Program` class could simply be:

```C#
class Program
{
    static void Main(string[] args)
    {
        Cli.Builder
            .SetDescription("A demo CLI application")
            .SetNameConvention(NameConvention.KebabCase)
            .SetResolver(GetService)
            .AddService<IGroupService>()
            .AddService<IUserService>()
            .AddExtensions()
            .AddOutputs()
            .Execute(args);
    }
}
```

This snippet creates the `Cli` instance, gives it a description and a resolver to create your services, then registers the services, static extensions for those interfaces, and any `Output` classes you have defined. Finally it runs execute with the console arguments, which allows the `Cli` class to use the services and methods it has to decide what to do.

### `SetDescription`

Sets some description text, which is included in help information. The app name is determined from the exe filename.

### `SetNameConvention`

Sets how service, method and parameter names are formatted. The currently available conventions are:

 - KebabCase: Formats like "save-xml-file" (default)
 - SnakeCase: Formats like "save_xml_file"

### `SetResolver`

Sets the resolver to use, either an implementation of `IResolver` or a `Func<Type, object>` function. If no resolver is provided, it uses `Activator.CreateInstance` to attempt to create services.

### `AddService`

Adds the specified service type to the `Cli` instance. It will scrape service information from an applied `CliServiceAttribute` if available, else determine the name from the interface name (removing prefix 'I' for interfaces, and suffix 'Service'). It will add all methods from the interface/class, included those inherited, except for `object` methods, scraping metadata from any found `CliMethodAttribute`s applied. Methods can be omited by adding `CliIgnoreAttribute` to those methods. Methods without a name defined in an attribute will use the method name, removing any 'Async' suffix found.

### `AddExtentions`

Searches all available assemblies for extension methods for any of the added service interfaces, and adds them. The `CliMethodAttribute` and `CliIgnoreAttribute` rules apply as normal. _This should be called after all services have been registered._

### `AddOutputs`

Searches all available assemblies for classes inherited from `Output` and adds them. The output class allows you to customize how method outputs are written. The AutoCli library knows how to output scalar values, enumerables, and classes by default, but the solution is 'one size fits all'. Outputs are particularly good for handling common wrapper classes, such as those with HTTP status information where the content (when successful) is in an inner property.

### `AddOutput`

Allows you to add a single `Output` class to the `Cli` instance, in a similar way to adding all using `AddOutputs`.

### `Execute`

The primary method of the `Cli` class, should be provided with the command line arguments. This method will match the arguments (in order) against the service, method, and then parameters, to determine which method overload to invoke. If none is found appropriate help information is shown, using the services, methods, and parameter combinations registered. Otherwise the method is invoked, and the result is output, first inspecting the `Output` classes available, and falling back to built-in output mechanisms.

## Roadmap

* Input classes/structs
* Input/output YAML/JSON/XML/other file data
* Clear method alias overloads (different parameter sets)
* Default parameters (unnamed)
* Powershell completion
* Unit testing
* Wiki?
