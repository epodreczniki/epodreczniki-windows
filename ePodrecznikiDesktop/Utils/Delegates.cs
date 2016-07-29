using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ePodrecznikiDesktop
{
    public delegate void HandbookDelegate(PageType pageType, int? bookId);
    public delegate void TermsDelegate();
    public delegate void ShowHideTitlebarDelegate(bool show);
}
