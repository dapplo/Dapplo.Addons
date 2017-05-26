# Costura support

As described in [Composition](composition.md), Dapplo.Addons can help you with that.

Although this works quite well when the assemblies (dll's) are stored on the file system, sometimes it would be good to be able to release a single executable. For this, a possible solution would be using [Costura](https://github.com/Fody/Costura). Costura will embed all the DLL's in your project into the resulting assembly (.exe or event .dll). When starting your application, Costura will register an Assembly resolver which takes care or unpacking the embedded files if they are needed.

Now there is a problem, as there are no direct references in your code (which is good for composition) the resolver will never know what to load and the assemblies are not available on the file system itself.

For this the @Dapplo.Addons.Bootstrapper.CompositionBootstapper was extended by using, via an unfortunately undocumented class, functionaliy of Costura. The code is wrapped in the @Dapplo.Addons.Bootstrapper.Internal.CosturaHelper class, and will automatically take care of finding the embedded resources when FindAndLoadAssemblies is used.

You can still supply the addons on the file system, which does make sense for many use cases.

If you want to have your assemblies added during the build process of Costura you can add these assemblies via your .csproj as EmbeddedResource. You will need to reference the resulting assembly, make sure it's build first, and specify "costura." as prefix in the link name.

An example is here, notice the "costura." prefix:

```xml
  <ItemGroup>
    <EmbeddedResource Include="..\Application.Demo.Addon\bin\$(Configuration)\Application.Demo.Addon.dll">
      <Link>costura.application.demo.addon.dll</Link>
    </EmbeddedResource>
    <EmbeddedResource Include="..\Application.Demo.MetroAddon\bin\$(Configuration)\Application.Demo.MetroAddon.dll">
      <Link>costura.application.demo.metroaddon.dll</Link>
    </EmbeddedResource>
    <EmbeddedResource Include="..\Application.Demo.OverlayAddon\bin\$(Configuration)\Application.Demo.OverlayAddon.dll">
      <Link>costura.application.demo.overlayaddon.dll</Link>
    </EmbeddedResource>
  </ItemGroup>
```

One disadvantage of this, is that the embedded assemblies are not compressed.

Background on this can be found here: https://github.com/Fody/Costura/issues/205
