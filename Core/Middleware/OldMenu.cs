using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using DAL;
using BLL.Middleware;
using BLL.Core.Domain;

namespace BLL.Core.Middleware
{
    public class OldMenu
    {
        bool Initialized = false;
        private SharedContext _context;
        public OldMenu(DbContext SharedContext)
        {
            Init(SharedContext);
        }
        private void Init(DbContext SharedContext)
        {
            try
            {
                _context = (SharedContext)SharedContext;
                Initialized = true;
            }
            catch(Exception ex)
            {
                string message = ex.Message;
            }
        }
        private List<int> UserAccessMenuList(int UserId)
        {
            return _context.USER_GROUP_OBJECT.Where(m => m.USER_GROUP.USER_GROUP_ASSIGN.Any(p => p.user_auto == UserId)).Select(k=> k.object_auto).Distinct().ToList();
        }
        public TopMenu GetMenuForUser(int UserId)
        {
            var result = new TopMenu {
                Id = 0,
                DivCssClass = "navbar",
                Divisions = new List<TopMenuDivision>()
            };
            var userObjects = UserAccessMenuList(UserId);
            var menu1 = _context.MENU_L1.Where(m => userObjects.Any(k => k == m.object_auto) && m.active).OrderBy(m => m.sorder);
            var menu2 = _context.MENU_L2.Where(m => m.active && userObjects.Any(k => k == m.object_auto) && menu1.Any(k=> k.menu_L1_auto == m.menu_L1_auto)).OrderBy(m => m.sorder);
            var menu3 = _context.MENU_L3.Where(m => m.active && userObjects.Any(k => k == m.object_auto) && menu2.Any(k => k.menu_L2_auto == m.menu_L2_auto)).OrderBy(m => m.sorder);
            var leftDivision = new TopMenuDivision {
                Id = 1,
                DivCssClass = "navbar-left",
                UlCssClass = "menu-container",
                OrderIndex = 1,
                levelOneList = new List<LevelOne>()
            };
            var rightDivision = new TopMenuDivision
            {
                Id = 2,
                DivCssClass = "navbar-right",
                UlCssClass = "menu-container",
                OrderIndex = 1,
                levelOneList = new List<LevelOne>()
            };

            foreach (var level1Menu in menu1.ToList())
            {
                var levelOne = new LevelOne
                {
                    Id = level1Menu.menu_L1_auto,
                    isMenuNotLink = false,
                    LiCssClass = "menu-item",
                    OrderIndex = 0,
                    Span = new TopMenuSpan
                    {
                        IconCssClass = "material-icons arrow",
                        IconText = "&#xE313;",
                        OrderIndex = level1Menu.sorder == null ? 99 : (int)level1Menu.sorder,
                        SpanText = level1Menu.label
                    },
                    levelTwoList = new List<LevelTwo>(),
                    Link = new TopMenuLink()
                };
                if (HasSubMenu(level1Menu.menu_L1_auto, menu2.Select(m => m.menu_L1_auto).ToList()))
                {
                    levelOne.isMenuNotLink = true;
                    foreach (var level2Menu in menu2.Where(m=>m.menu_L1_auto == level1Menu.menu_L1_auto).ToList())
                    {
                        var levelTwo = new LevelTwo
                        {
                            Id = level2Menu.menu_L2_auto,
                            isMenuNotLink = false,
                            ParentId = level1Menu.menu_L1_auto,
                            UlCssClass = "sub-menu-container",
                            LiCssClass = "sub-menu-item",
                            Span = new TopMenuSpan { IconCssClass = "material-icons arrow",
                             IconText = "&#xE313;",
                             OrderIndex = level2Menu.sorder == null ? 99 : (int)level2Menu.sorder,
                             SpanText = level2Menu.label
                            },
                            levelThreeList = new List<LevelThree>(),
                            Link = new TopMenuLink()
                        };
                        if (HasSubMenu(level2Menu.menu_L2_auto, menu3.Select(m => m.menu_L2_auto).ToList()))
                        { //Level3 Links
                            levelTwo.isMenuNotLink = true;
                            foreach (var level3Menu in menu3.Where(m => m.menu_L2_auto == level2Menu.menu_L2_auto).ToList())
                            {


                                var levelThree = new LevelThree
                                {
                                    Id = level3Menu.menu_L3_auto,
                                    ParentId = level2Menu.menu_L2_auto,
                                    UlCssClass = "sub-menu-container",
                                    LiCssClass = "sub-menu-item",
                                    Span = new TopMenuSpan
                                    {
                                        IconCssClass = "material-icons arrow",
                                        IconText = "&#xE313;",
                                        OrderIndex = level2Menu.sorder == null ? 99 : (int)level2Menu.sorder,
                                        SpanText = level2Menu.label
                                    },
                                    Link = new TopMenuLink {
                                        Id = level3Menu.menu_L3_auto,
                                        Text = level3Menu.label,
                                        Href = level3Menu.targetpath,
                                        OrderIndex = level3Menu.sorder == null ? 99 : (int)level3Menu.sorder,
                                        OpenInNewWindow = level3Menu.new_window
                                    }
                                };
                                levelTwo.levelThreeList.Add(levelThree);
                            }
                        }
                        else //Level2 Links
                        {
                            levelTwo.Link = new TopMenuLink {
                                Id = level2Menu.menu_L2_auto,
                                Text = level2Menu.label,
                                Href = level2Menu.targetpath,
                                OrderIndex = level2Menu.sorder == null ? 99 : (int)level2Menu.sorder,
                                OpenInNewWindow = level2Menu.new_window
                            };
                        }
                        levelOne.levelTwoList.Add(levelTwo);
                    }
                }
                else //Level1 Links
                {
                    levelOne.Link = new TopMenuLink
                    {
                        Id = level1Menu.menu_L1_auto,
                        Text = level1Menu.label,
                        Href = level1Menu.targetpath,
                        OrderIndex = level1Menu.sorder == null ? 99 : (int)level1Menu.sorder,
                        OpenInNewWindow = true
                    };
                }
                if (levelOne.Id == 7 || levelOne.Id == 8)
                    rightDivision.levelOneList.Add(levelOne);
                else
                leftDivision.levelOneList.Add(levelOne);
            }

            result.Divisions.Add(leftDivision);
            result.Divisions.Add(rightDivision);
            return result;
        }

        private bool HasSubMenu(int Id, List<int?> SubMenuIds)
        {
            return SubMenuIds.Any(m => m == Id);
        }
    }
}