﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace TwitterPluginForAtomSite
{
    public class TwitterInteraction
    {
        public static TwitterStructs.Twitter GetUpdates(
            int CacheDuration,
            string TwitterName,
            int Limit,
            int PagingIndex)
        {
            var cached = TwitterCacheManager.Get<TwitterStructs.Twitter>(
                TwitterStructs.TwitterConsts.TwitterCurrentTweets + TwitterName + PagingIndex.ToString(),
                CacheDuration);
            if (cached != null)
                return cached;
            var webClient = new System.Net.WebClient();
            string url = string.Format("http://twitter.com/status/user_timeline/{0}.xml?count={1}&page={2}",
                TwitterName,
                Limit,
                PagingIndex);
            string resultXML = string.Empty;
            try
            {
                resultXML = webClient.DownloadString(url);
            }
            catch
            {
                return null;
            }
            if (string.IsNullOrEmpty(resultXML))
                return null;

            XDocument result = null;
            try
            {
                result = XDocument.Parse(resultXML);
            }
            catch
            {
                return null;
            }
            if (result == null)
                return null;
            var toReturn = new TwitterStructs.Twitter();
            var statusArray = result.Descendants("status");
            var userElement = statusArray.Take(1).Descendants("user").SingleOrDefault();
            toReturn.User = GetTwitterUserFromUserElement(userElement);
            var tweets = from tw in statusArray
                         let dateParts =
                         ElementValueSingleOrDefault(tw, "created_at").Split(' ')
                         let replyToScreenName =
                         ElementValueSingleOrDefault(tw, "in_reply_to_screen_name")
                         select new TwitterStructs.Tweet
                         {
                             CreatedAt = DateTime.Parse(
                                 string.Format("{0} {1} {2} {3} GMT",
                                 dateParts[1],
                                 dateParts[2],
                                 dateParts[5],
                                 dateParts[3]),
                                 System.Globalization.CultureInfo.InvariantCulture),
                             Source = ElementValueSingleOrDefault(tw, "source"),
                             Text = AddMarkupToTweet(ElementValueSingleOrDefault(tw, "text")),
                             InReplyToScreenName = replyToScreenName,
                             InReplyToStatusURL = !string.IsNullOrEmpty(replyToScreenName) ? string.Format("http://twitter.com/{0}/status/{1}", replyToScreenName, ElementValueSingleOrDefault(tw, "in_reply_to_status_id")) : ""
                         };
            toReturn.Tweets = tweets.ToList();
            TwitterCacheManager.Set(toReturn,
                TwitterStructs.TwitterConsts.TwitterCurrentTweets + TwitterName + PagingIndex.ToString());
            return toReturn;
        }

        private static string ElementValueSingleOrDefault(XElement element, string name)
        {
            return element.Element(name) != null && !string.IsNullOrEmpty(element.Element(name).Value) ? element.Element(name).Value : "";
        }

        private static TwitterStructs.User GetTwitterUserFromUserElement(XElement u)
        {
            return new TwitterStructs.User
                               {
                                   ID = ElementValueSingleOrDefault(u, "id"),
                                   ImageURL = ElementValueSingleOrDefault(u, "profile_image_url"),
                                   ScreenName = ElementValueSingleOrDefault(u, "screen_name"),
                                   HomeURL = ElementValueSingleOrDefault(u, "url"),
                                   Name = ElementValueSingleOrDefault(u, "name"),
                                   TwitterHomeURL = string.Format("http://twitter.com/{0}", ElementValueSingleOrDefault(u, "screen_name")),
                                   Description = ElementValueSingleOrDefault(u, "description"),
                                   Followers = ElementValueSingleOrDefault(u, "followers_count"),
                                   FriendsCount = ElementValueSingleOrDefault(u, "friends_count"),
                                   StatusCount = ElementValueSingleOrDefault(u, "statuses_count")
                               };
        }

        public static TwitterStructs.User GetTwitterUser(int CacheDuration, string TwitterName)
        {
            var cached = TwitterCacheManager.Get<TwitterStructs.User>(
                TwitterStructs.TwitterConsts.TwitterUser + TwitterName,
                CacheDuration);
            if (cached != null)
                return cached;

            var webClient = new System.Net.WebClient();
            string url = string.Format("http://twitter.com/users/show/{0}.xml", TwitterName);
            string resultXML = string.Empty;
            try
            {
                resultXML = webClient.DownloadString(url);
            }
            catch
            {
                return null;
            }
            if (string.IsNullOrEmpty(resultXML))
                return null;

            XDocument result = null;
            try
            {
                result = XDocument.Parse(resultXML);
            }
            catch
            {
                return null;
            }
            if (result == null)
                return null;

            var toReturn = GetTwitterUserFromUserElement(result.Elements().SingleOrDefault());
            TwitterCacheManager.Set(toReturn, TwitterStructs.TwitterConsts.TwitterUser + TwitterName);
            return toReturn;
        }

        public static string AddMarkupToTweet(string Tweet)
        {
            string tagRegex = @"(\#)\w+[\w]";
            string mentionRegex = @"(\@)\w+[\w]";
            string linkRegex = @"((https|http):\/\/+[\w\d\.\-_\?\&\%\/\=\#\:]*)";

            DoForAllMatches(Tweet, linkRegex, p =>
            {
                Tweet = Tweet.Replace(p, string.Format("<span class=\"TwitterStatusLink\"><a href=\"{0}\">{0}</a></span>", p));
            });
            DoForAllMatches(Tweet, tagRegex, p =>
            {
                Tweet = Tweet.Replace(p, string.Format("<span class=\"TwitterStatusTag\"><a href=\"http://twitter.com/search?q=%23{0}\">{1}</a></span>", p.Substring(1, p.Length - 1), p));
            });
            DoForAllMatches(Tweet, mentionRegex, p =>
            {
                Tweet = Tweet.Replace(p, string.Format("<span class=\"TwitterStatusMention\">@<a href=\"http://twitter.com/{0}\">{0}</a></span>", p.Substring(1, p.Length - 1)));
            });

            return Tweet;
        }

        private static void DoForAllMatches(string Input, string Regex, Action<string> ToDo)
        {
            System.Text.RegularExpressions.Regex reg = new System.Text.RegularExpressions.Regex(Regex);
            //There has got to be a better way to do this..
            foreach (System.Text.RegularExpressions.Match caught in reg.Matches(Input))
            {
                foreach (System.Text.RegularExpressions.Capture capture in caught.Captures)
                {
                    ToDo(capture.Value);
                }
            }
        }
    }


}
