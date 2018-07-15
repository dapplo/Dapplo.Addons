Dapplo.Addons
=====================
Work in progress

- Documentation can be found [here](http://www.dapplo.net/blocks/Dapplo.Addons) (later!!)
- Current build status: [![Build status](https://ci.appveyor.com/api/projects/status/bem7losuu07ywvyr?svg=true)](https://ci.appveyor.com/project/dapplo/dapplo-addons)
- Coverage Status: [![Coverage Status](https://coveralls.io/repos/github/dapplo/Dapplo.Addons/badge.svg?branch=master)](https://coveralls.io/github/dapplo/Dapplo.Addons?branch=master)
- NuGet package: [![NuGet package](https://badge.fury.io/nu/Dapplo.Addons.svg)](https://badge.fury.io/nu/Dapplo.Addons)

This library can be used to host addons in your application, or make addons for your application.

This project was mainly created for Greenshot, here is a subset of the code how Greenshot starts:

```
// Configure your application
var applicationConfig = ApplicationConfigBuilder
    .Create()
	// Used for logging, configuration, and thread names
	.WithApplicationName("Greenshot")
	// Used to prevent multiple instances
	.WithMutex("<your GUID>")
	// Enable Dapplo.Ini & Dapplo.Language support, these packages need to be added via nuget.
	.WithConfigSupport()
	// Enable support for IniSection resolving, no need to register them manually
	.WithIniSectionResolving()
	// Enable support for ILanguage resolving, no need to register them manually
	.WithLanguageResolving()
	// Add directories to scan for dlls
	.WithScanDirectories("... directory ...")
	// Scan for all the assemblies, in the exe directory or specified scan directories, called Greenshot.Addon.*.dll
	.WithAssemblyPatterns("Greenshot.Addon.*")
	.BuildApplicationConfig();

// Bootstrap it
using (var bootstrapper = new ApplicationBootstrapper(applicationConfig))
{
	if (bootstrapper.IsAlreadyRunning) {
		// Exit as we are already running
	}
	// this starts all services you registered (implement IStartup or IStartupAsync, and registered as IService in the AddonModule)
	await bootstrapper.StartupAsync();
	
	// you can use the configured Autofac container, if needed:
	bootstrapper.Container.Resolve...
	
	// Wait, if needed
}
// Shutdown of your services is automatically called when dispose is called
```
Every addon needs to have at least one class extending AddonModule, which is practically the same as an Autofac Module.

Example Service:

```
[Service(nameof(SomeAddonService), nameof(DependsOn))]
public class SomeAddonService : IStartupAsync, IShutdownAsync
{
	public async Task ShutdownAsync(CancellationToken cancellationToken = default)
	{
		// Shutdown code
		await Task.Delay(100, cancellationToken);
	}

	public async Task StartupAsync(CancellationToken cancellationToken = default)
	{
		// Startup code
		await Task.Delay(100, cancellationToken);
	}
}
```

Example AddonModule:

```
public class ExampleAddonModule : AddonModule
{
	protected override void Load(ContainerBuilder builder)
	{
		builder
			.RegisterType<SomeAddonService>()
			.As<IService>()
			.SingleInstance();
	}
}
```

Also look [here](https://github.com/dapplo/Dapplo.Addons/blob/master/src/Dapplo.Addons.Tests/ApplicationBootstrapperTests.cs#L138) for an example Test-Case on how to use this.

Every addon should use Dapplo.Addons as a reference, the containing application should use Dapplo.Addons.Bootstrapper
It is heavily based upon Autofac, and can use the Dapplo.Config framework for inserting translations & configurations in your classes.
The Dapplo.CaliburnMicro project extends this functionality, to have a MVVM application with Composition.
