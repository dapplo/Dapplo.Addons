using Autofac;
using Autofac.Core;
using Dapplo.Log;

namespace Dapplo.Addons.Bootstrapper
{
    /// <summary>
    /// A module to enable logs
    /// </summary>
    public class LogRequestModule : Module
    {
        private static readonly LogSource Log = new LogSource();
        private int _depth;

        /// <summary>
        /// Implement the AttachToComponentRegistration, to register logging
        /// </summary>
        /// <param name="componentRegistry">IComponentRegistry</param>
        /// <param name="registration">IComponentRegistration</param>
        protected override void AttachToComponentRegistration(IComponentRegistry componentRegistry, IComponentRegistration registration)
        {
            registration.Preparing += RegistrationOnPreparing;
            registration.Activating += RegistrationOnActivating;
            registration.Activated += RegistrationOnActivated;
            base.AttachToComponentRegistration(componentRegistry, registration);
        }

        private void RegistrationOnActivated(object sender, IActivatedEventArgs<object> e)
        {
            Log.Debug().WriteLine("{0}Activated {1}", GetPrefix(), e.Component.Activator.LimitType);
        }

        private string GetPrefix()
        {
            return new string('-', _depth * 2);
        }

        private void RegistrationOnPreparing(object sender, PreparingEventArgs preparingEventArgs)
        {
            Log.Debug().WriteLine("{0}Resolving  {1}", GetPrefix(), preparingEventArgs.Component.Activator.LimitType);
            _depth++;
        }

        private void RegistrationOnActivating(object sender, ActivatingEventArgs<object> activatingEventArgs)
        {
            _depth--;
            Log.Debug().WriteLine("{0}Activating {1}", GetPrefix(), activatingEventArgs.Component.Activator.LimitType);
        }
    }
}
