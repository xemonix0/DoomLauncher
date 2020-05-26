﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DoomLauncher.Interfaces;
using DoomLauncher.DataSources;
using Equin.ApplicationFramework;
using System.Globalization;

namespace DoomLauncher
{
    public partial class BasicTabViewCtrl : UserControl, ITabView, ICloneable
    {
        protected string m_title;
        protected object m_key;
        protected GameFileFieldType[] m_selectFields;
        protected GameFileViewFactory m_factory;

        private IGameFileView m_gameFileView;

        public BasicTabViewCtrl(object key, string title, IGameFileDataSourceAdapter adapter, GameFileFieldType[] selectFields, GameFileViewFactory factory)
            : this (key, title, adapter, selectFields, factory.CreateGameFileView())
        {
            m_factory = factory;
        }

        protected BasicTabViewCtrl(object key, string title, IGameFileDataSourceAdapter adapter, GameFileFieldType[] selectFields, IGameFileView view)
        {
            InitializeComponent();
            m_key = key;
            m_title = title;
            Adapter = adapter;
            m_selectFields = selectFields.ToArray();

            UserControl ctrl = (UserControl)view;

            ctrl.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            ctrl.Dock = DockStyle.Fill;
            Controls.Add(ctrl);
            m_gameFileView = (IGameFileView)ctrl;
        }

        public virtual object Clone()
        {
            // TODO this is also dumb
            BasicTabViewCtrl view = new BasicTabViewCtrl(m_key, m_title, Adapter, m_selectFields, m_factory.CreateGameFileViewGrid());
            SetBaseCloneProperties(view);
            return view;
        }

        protected void SetBaseCloneProperties(ITabView view)
        {
            if (GameFileViewControl is IGameFileColumnView columnView)
                view.SetColumnConfig(columnView.ColumnFields, GetColumnConfig().ToArray());
            else
                view.SetColumnConfig(GameFileViewFactory.DefaultColumnTextFields, DataCache.Instance.GetColumnConfig());
        }

        public List<ColumnConfig> GetColumnConfig()
        {
            if (GameFileViewControl is IGameFileColumnView columnView)
            {
                List<ColumnConfig> config = new List<ColumnConfig>();

                foreach (string key in columnView.GetColumnKeyOrder())
                {
                    ColumnConfig col = new ColumnConfig(Title, key,
                        columnView.GetColumnWidth(key), columnView.GetColumnSort(key));
                    config.Add(col);
                }

                return config;
            }
            else
            {
                throw new InvalidOperationException("GameFileViewControl is not IGameFileColumView");
            }
        }

        public void SetColumnConfig(ColumnField[] columnTextFields, ColumnConfig[] colConfig)
        {
            if (GameFileViewControl is IGameFileColumnView columnView)
            {
                columnView.SuspendLayout();

                var sortedColumns = SortColumns(Title, columnTextFields, colConfig);
                columnView.SetColumnFields(sortedColumns);
                SetSortedColumn(Title, columnView, colConfig);
                SetColumnWidths(Title, columnView, colConfig);

                columnView.SetColumnFormat("ReleaseDate", CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                columnView.SetColumnFormat("Downloaded", CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);
                columnView.SetColumnFormat("LastPlayed", CultureInfo.CurrentCulture.DateTimeFormat.ShortDatePattern);

                GameFileViewControl.ResumeLayout();
            }
        }

        private static ColumnField[] SortColumns(string tab, ColumnField[] items, ColumnConfig[] config)
        {
            List<ColumnField> ret = new List<ColumnField>();
            //Pre 1.1.0 update check. Without this maps will be thrown on the end of the column list
            bool setMapLocation = config.Length > 0 && !config.Any(x => x.Column == "MapCount");

            foreach (ColumnConfig configItem in config)
            {
                var check = items.FirstOrDefault(x =>
                    configItem.Parent == tab && x.DataKey.Equals(configItem.Column, StringComparison.InvariantCultureIgnoreCase));

                if (check != null)
                    ret.Add(check);
            }
            //continue with pre 1.1.0 check (this can probably be removed at this point, though)
            if (setMapLocation && ret.Count > 4)
            {
                var maps = items.FirstOrDefault(x => x.Title.Equals("maps", StringComparison.InvariantCultureIgnoreCase));

                if (maps != null)
                {
                    ret.Remove(maps);
                    ret.Insert(4, maps);
                }
            }

            return ret.Union(items).ToArray();
        }

        private static void SetColumnWidths(string tab, IGameFileColumnView ctrl, ColumnConfig[] config)
        {
            IEnumerable<ColumnConfig> configSet = config.Where(x => x.Parent == tab);
            foreach (ColumnConfig col in configSet)
            {
                ctrl.SetColumnWidth(col.Column, col.Width);
            }
        }

        private static void SetSortedColumn(string tab, IGameFileColumnView gameFileViewControl, ColumnConfig[] colConfig)
        {
            ColumnConfig config = colConfig.FirstOrDefault(x => x.Parent == tab && x.Sort != SortOrder.None);

            if (config != null)
                gameFileViewControl.SetSortedColumn(config.Column, config.Sort);
        }

        public IGameFileView GameFileViewControl { get { return m_gameFileView; } }

        public object Key { get { return m_key; } }

        protected virtual bool FilterIWads { get { return true; } }

        public virtual void SetGameFiles()
        {
            GameFileGetOptions options = new GameFileGetOptions();
            options.SelectFields = m_selectFields;
            SetDataSource(Adapter.GetGameFiles(options));
        }

        public virtual void SetGameFiles(IEnumerable<GameFileSearchField> searchFields)
        {
            IEnumerable<IGameFile> items = new IGameFile[0];

            foreach (GameFileSearchField sf in searchFields)
            {
                items = items.Union(Adapter.GetGameFiles(new GameFileGetOptions(m_selectFields, sf)));
            }

            SetDataSource(items);
        }

        public void SetGameFilesData(IEnumerable<IGameFile> gameFiles)
        {
            SetDataSource(gameFiles);
        }

        public virtual void UpdateDataSourceFile(IGameFile gameFile)
        {
            if (m_gameFileView.DataSource != null)
            {
                foreach (IGameFile item in m_gameFileView.DataSource)
                {
                    if (item.Equals(gameFile))
                    {
                        Array.ForEach(item.GetType().GetProperties().Where(x => x.SetMethod != null).ToArray(), x => x.SetValue(item, x.GetValue(gameFile)));
                        // TODO
                        ((UserControl)m_gameFileView).Invalidate(true);
                        break;
                    }
                }
            }
        }

        protected void SetDisplayText(string text)
        {
            m_gameFileView.SetDisplayText(text);
        }

        protected void SetDataSource(IEnumerable<IGameFile> gameFiles)
        {
            if (FilterIWads && !(Adapter is IdGamesDataAdapater))
                gameFiles = gameFiles.Except(Adapter.GetGameFileIWads());

            if (!gameFiles.Any())
            {
                m_gameFileView.DataSource = null;
                m_gameFileView.SetDisplayText("No Results Found");
            }
            else
            {
                m_gameFileView.DataSource = gameFiles.Cast<GameFile>().ToList();
            }
        }

        protected IGameFile FromDataBoundItem(object item)
        {
            return ((ObjectView<GameFile>)item).Object as IGameFile;
        }

        public virtual bool IsLocal { get { return true; } }
        public virtual bool IsEditAllowed { get { return true; } }
        public virtual bool IsDeleteAllowed { get { return true; } }
        public virtual bool IsSearchAllowed { get { return true; } }
        public virtual bool IsPlayAllowed { get { return true; } }
        public virtual bool IsAutoSearchAllowed { get { return true; } }
        public string Title { get { return m_title; } }
        public virtual IGameFileDataSourceAdapter Adapter { get; set; }
    }
}
