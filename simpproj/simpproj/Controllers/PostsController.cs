﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using NHibernate.Linq;
using simpproj.Models;
using simpproj.ViewModels;
using simpproj.Infrastructure;
using System.Text.RegularExpressions;

namespace simpproj.Controllers
{
    public class PostsController : Controller
    {
        private const int PostsPerPage = 10;

        public ActionResult Index(int page = 1)
        {
            var baseQuery = Database.Session.Query<Post>().Where(t => t.DeletedAt == null).OrderByDescending(t => t.CreatedAt);

            var totalPostCount = baseQuery.Count();
            var postIds = baseQuery.Skip((page - 1) * PostsPerPage).Take(PostsPerPage).Select(t => t.Id).ToArray();
            var posts = baseQuery.Where(t => postIds.Contains(t.Id)).FetchMany(t => t.Tags).Fetch(t => t.User).ToList();

            return View(new PostsIndex
            {
                Posts = new PagedData<Post>(posts, totalPostCount, page, PostsPerPage)
            });
        }
        
        // Will need to create generator on the RoutesConfig cs
        public ActionResult Show(string idAndSlug)
        {
            var parts = SeparateIdAndSlug(idAndSlug);
            if (parts == null)
                return HttpNotFound();

            var post = Database.Session.Load<Post>(parts.Item1);
            if (post == null || post.IsDeleted)
                return HttpNotFound();

            // Redirect to the correct url if the entered slug or id is incorrect.
            if (!post.Slug.Equals(parts.Item2, StringComparison.CurrentCultureIgnoreCase))
                return RedirectToRoutePermanent("Post", new { id = parts.Item1, slug = post.Slug});

            return View(new PostsShow
            {
                Post = post
            });

        }

        private System.Tuple<int, string> SeparateIdAndSlug(string idAndSlug)
        {
            var matches = Regex.Match(idAndSlug, @"^(\d+)\-(.*)?$");
            if (!matches.Success)
                return null;

            // Pulls out first group of regular expression
            var id = int.Parse(matches.Result("$1"));
            // Pulls out second group
            var slug = matches.Result("$2");
            return Tuple.Create(id, slug);
        }

        public ActionResult Tag(string idAndSlug, int page = 1)
        {
            var parts = SeparateIdAndSlug(idAndSlug);
            if (parts == null)
                return HttpNotFound();

            var tag = Database.Session.Load<Tag>(parts.Item1);
            if (tag == null)
                return HttpNotFound();

            if (!tag.Slug.Equals(parts.Item2, StringComparison.CurrentCultureIgnoreCase))
                return RedirectToRoutePermanent("tag", new { id = parts.Item1, slug = tag.Slug });

            var totalPostCount = tag.Posts.Count();
            var postIds = tag.Posts
                .OrderByDescending(g => g.CreatedAt)
                .Skip((page - 1) * PostsPerPage)
                .Take(PostsPerPage)
                .Where(t => t.DeletedAt == null)
                .Select(t => t.Id)
                .ToArray();

            var posts = Database.Session.Query<Post>()
                .OrderByDescending(b => b.CreatedAt)
                .Where(t => postIds.Contains(t.Id))
                .FetchMany(f => f.Tags)
                .Fetch(f => f.User)
                .ToList();

            return View(new PostsTag
            {
                Tag = tag,
                Posts = new PagedData<Post>(posts, totalPostCount, page, PostsPerPage)
            });
        }

    }
}