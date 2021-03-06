﻿#region Copyright (c) Lokad 2009-2010
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Web.Mvc;
using System.Web.Security;
using DotNetOpenAuth.Messaging;
using DotNetOpenAuth.OpenId;
using DotNetOpenAuth.OpenId.RelyingParty;
using Lokad.Cloud.Snapshot.Cloud.UI.Authorization;

namespace Lokad.Cloud.Snapshot.Cloud.UI.Controllers
{
	public class AccountController : Controller
	{
		private static readonly OpenIdRelyingParty OpenId = new OpenIdRelyingParty();

		public ActionResult Login()
		{
			return View();
		}

		public ActionResult Authenticate(string returnUrl)
		{
			var response = OpenId.GetResponse();

			if (response == null)
			{
				// Stage 2: user submitting Identifier
				Identifier id;
				if (Identifier.TryParse(Request.Form["openid_identifier"], out id))
				{
					try
					{
						OpenId.CreateRequest(Request.Form["openid_identifier"]).RedirectToProvider();
					}
					catch (ProtocolException)
					{
						ViewData["Message"] = "No such endpoint can be found.";
						return View("Login");
					}
				}
				else
				{
					ViewData["Message"] = "Invalid identifier.";
					return View("Login");
				}
			}
			else
			{
				// HACK: Filtering users based on their registrations
				if (!Users.IsAdministrator(response.ClaimedIdentifier))
				{
					ViewData["Message"] = "This user does not have access rights.";
					return View("Login");
				}

				// Stage 3: OpenID Provider sending assertion response
				switch (response.Status)
				{
					case AuthenticationStatus.Authenticated:
						Session["FriendlyIdentifier"] = response.FriendlyIdentifierForDisplay;
						FormsAuthentication.SetAuthCookie(response.ClaimedIdentifier, false);
						if (!string.IsNullOrEmpty(returnUrl))
						{
							return Redirect(returnUrl);
						}
						else
						{
							return RedirectToAction("Index", "Snapshots");
						}
					case AuthenticationStatus.Canceled:
						ViewData["Message"] = "Canceled at provider";
						return View("Login");
					case AuthenticationStatus.Failed:
						ViewData["Message"] = response.Exception.Message;
						return View("Login");
				}
			}
			return new EmptyResult();
		}

		public ActionResult Logout()
		{
			FormsAuthentication.SignOut();
			return RedirectToAction("Index", "Snapshots");
		}

	}
}
