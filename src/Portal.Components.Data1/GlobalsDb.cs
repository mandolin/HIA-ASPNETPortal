using System.Collections.Generic;
using System.Linq;

namespace ASPNET.StarterKit.Portal
{
    public class GlobalsDb : IGlobalsDb
    {
        private readonly PortalCfgDbContext _context;
        private List<GlobalItem> _items;

        public GlobalsDb(PortalCfgDbContext context)
        {
            _context = context;
            _items = _context.Globals.ToList();
        }

        #region IGlobalsDb Members

        public IGlobalItem GetSinglePortal(int portalId)
        {
            return _items.Single(i => i.PortalId == portalId);
        }


        public void UpdatePortalInfo(int portalId, string portalName, bool alwaysShow)
        {
            IGlobalItem globalRow = _items.Single(i => i.PortalId == portalId);

            globalRow.PortalId = portalId;
            globalRow.PortalName = portalName;
            globalRow.AlwaysShowEditButton = alwaysShow;

            _context.SaveChanges();
            _items = _context.Globals.ToList();
        }

        #endregion
    }
}