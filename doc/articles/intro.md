# Dapplo.Addons


> [!WARNING]
> This is still work in progress

This project will help you to quickly make your application support dependency injection, with composition or addons.

Look [here](https://github.com/dapplo/Dapplo.Addons/blob/master/src/Dapplo.Addons.Tests/ApplicationBootstrapperTests.cs#L152) for an example Test-Case on how to use this.


The project introduces some interfaces and attributes to support making your application modular:
1. @Dapplo.Addons.IAsyncStartupAction or @Dapplo.Addons.IStartupAction (non async variant) for having something start together with your application. Your service will need to implement this interface, and apply a @Dapplo.Addons.StartupActionAttribute to the class.
2. @Dapplo.Addons.IAsyncShutdownAction or @Dapplo.Addons.IShutdownAction (non async variant) for having something shutdown together with your application. Your service will need to implement this interface, and apply a @Dapplo.Addons.ShutdownActionAttribute to the class.
3. @Dapplo.Addons.IServiceExporter can be used in your components to export other components at runtime, for instance you only want certain features to be available depending on system capabilities or user rights.
4. @Dapplo.Addons.IServiceRepository can be used in your components to have the framework load modules/components.
5. @Dapplo.Addons.IMefServiceLocator can be used in your components to find imports at runtime.
6. @Dapplo.Addons.IBootstrapper is the base interface for all the bootstrapper, it takes care that many interfaces are implemented. It also makes sure the bootstrappers implement Microsoft.Practices.ServiceLocation.IServiceLocator which makes it possible to use the Dapplo Bootstrappers with different frameworks.

The bootstrappers:
1. @Dapplo.Addons.CompositionBootstrapper is the basic bootstrapper, to setup the MEF container and load & supply components.
2. @Dapplo.Addons.StartupShutdownBootstrapper extends the @Dapplo.Addons.CompositionBootstrapper with startup and shutdown functionality.
3. @Dapplo.Addons.ApplicationBootstrapper, which implements @Dapplo.Addons.IApplicationBootstrapper, and extends the @Dapplo.Addons.StartupShutdownBootstrapper with application functionality. It brings the concept of an application name, which is important for configuration / translations and adds mutex support (a single instance of your application).
