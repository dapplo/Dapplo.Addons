# Composition

The problem with composition, and using MEF, is that the container doesn't need to know up front what files to load.
This is solved in the Dapplo.Addons.Bootstrapper by supplying methods which can load addons from a directory.

The interface @Dapplo.Addons.IServiceRepository, extended by the @Dapplo.Addons.IBootstrapper, which implemented by the @Dapplo.Addons.Bootstrapper.CompositionBootstapper (and thus @Dapplo.Addons.Bootstrapper.ApplicationBootstrapper) makes a couple of methods available to support this scenario. For instance, you can call "FindAndLoadAssemblies" with a pattern describing the modules you want to load. An example for Greenshot would be FindAndLoadAssemblies("Greenshot.Addons.*") which will load all available addons.

Note:
If you want to reuse assemblies, if can help to set "use specific version" on the references to "false".
