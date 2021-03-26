using UnityEngine;

namespace Vrsys
{
    [RequireComponent(typeof(ViewingSetupHMDAnatomy))]
    public class NavigationBaseHMD : NavigationBase
    {
        protected ViewingSetupHMDAnatomy viewingSetupHMD
        {
            get
            {
                return viewingSetup as ViewingSetupHMDAnatomy;
            }
        }
    }
}
