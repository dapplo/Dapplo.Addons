Dapplo.Addons
=====================
Work in progress

Documentation can be found [here](http://www.dapplo.net/blocks/Dapplo.Addons.html) (later!!)
Current build status: [![Build status](https://ci.appveyor.com/api/projects/status/bem7losuu07ywvyr?svg=true)](https://ci.appveyor.com/project/dapplo/dapplo-addons)

This library can be used to host addons in your application, or make addons for your application.

Every addon should user Dapplo.Addons as a reference, the containing application should use Dapplo.Addons.Bootstrapper
It is heavily based upon MEF, and uses the Dapplo.Config framework for inserting translations & configurations in your classes.

Look [here](https://github.com/dapplo/Dapplo.Addons/blob/master/Dapplo.Addons.Tests/AddonTest.cs) for an example Test-Case on how to use this.

