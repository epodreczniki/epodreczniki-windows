using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace epodreczniki.Common
{
    public class NavigationPageParameter
    {
        public bool ShowAllAttributes
        {
            get; set;
        }

        public string BackPageTypeName
        {
            get; set;
        }

        public NavigationPageParameter(bool showAll, string pageType)
        {
            ShowAllAttributes = showAll;
            BackPageTypeName = pageType;
        }
    }
}
