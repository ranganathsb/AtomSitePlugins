﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AtomSite.WebCore;
using System.Web.Mvc;

namespace TwitterPluginForAtomSite
{
    public class TwitterController : AtomSite.WebCore.BaseController
    {
        [ScopeAuthorize]
        [AcceptVerbs(HttpVerbs.Post)]
        public ActionResult Setup(string id, int? limit, int? cachedurationinminutes, int? clientrefreshinminutes)
        {
            TwitterStructs.Settings current = TwitterPluginCore.UpdateAndReturnCurrent(id, limit, cachedurationinminutes, clientrefreshinminutes);
            if (Request.IsAjaxRequest())
                return Json(current);
            else
                return RedirectToRoute(new { controller = "Admin" });
        }

        public ActionResult Get(int? PagingIndex)
        {
            var response = TwitterPluginCore.GetUpdates(PagingIndex);
            if (Request.IsAjaxRequest())
                return PartialView("TwitterPartialWidget", response.Tweets);
            else
                return PartialView("TwitterWidget", new Models.ClientModel { TwitterResponse = response });
        }
    }
}
