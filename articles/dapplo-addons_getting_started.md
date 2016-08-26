This library can be used to host addons in your application, or make addons for your application.

Every addon should use Dapplo.Addons as a reference, the containing application should use Dapplo.Addons.Bootstrapper
It is heavily based upon MEF, and uses Dapplo.Utils for Assembly loading, the Dapplo.Config framework for inserting translations & configurations in your classes.

Look [here](https://github.com/dapplo/Dapplo.Addons/blob/master/Dapplo.Addons.Tests/AddonTest.cs) for an example Test-Case on how to use this.