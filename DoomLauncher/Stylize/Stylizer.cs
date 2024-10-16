﻿using System;
using System.Drawing;
using System.Windows.Forms;
using DoomLauncher.Controls;

namespace DoomLauncher
{
    public enum StylizerOptions
    {
        None,
        SetupTitleBar,
        StylizeTitleBar
    }

    internal class Stylizer
    {
        private readonly struct StylizeControlResults
        {
            public StylizeControlResults(bool loadRequired)
            {
                LoadRequired = loadRequired;
            }

            public readonly bool LoadRequired;
        }

        private static readonly IThemeColors CurrentTheme = ColorTheme.Current;

        public static void Stylize(Form form, bool designMode, StylizerOptions options = StylizerOptions.None)
        {
            StylizeInternal(form, designMode, options, null);
        }

        private static void StylizeInternal(Form form, bool designMode, StylizerOptions options, Func<Control, bool> shouldStylize)
        {
            bool registerLoad = false;
            if (designMode)
                return;

            if (options.HasFlag(StylizerOptions.SetupTitleBar))
                SetupTitleBar(form);

            foreach (Control control in form.Controls)
            {
                if (control is CRichTextBox)
                    registerLoad = true;

                var results = StylizeControlInternal(control, designMode, options, shouldStylize);
                registerLoad = registerLoad || results.LoadRequired;
            }

            if (!designMode && registerLoad)
                form.Load += Form_Load;
        }

        private static void Form_Load(object sender, EventArgs e)
        {
            Form form = (Form)sender;
            form.Load -= Form_Load;
            StylizeInternal(form, false, StylizerOptions.None, ShouldStylize);
        }

        private static bool ShouldStylize(Control control)
        {
            return control is CRichTextBox;
        }

        public static void SetupTitleBar(Form form)
        {
            form.ShowIcon = false;
            form.Load += Form_LoadDarkMode;
            var titleBars = form.Controls.Find("titleBar", true);
            if (titleBars.Length > 0 && titleBars[0].Parent is TableLayoutPanel tbl)
            {
                tbl.Controls.Remove(titleBars[0]);
                if (tbl.RowStyles.Count > 0)
                    tbl.RowStyles[0].Height = 0;
                form.Height -= titleBars[0].Height;
            }
        }

        private static void Form_LoadDarkMode(object sender, EventArgs e)
        {
            var form = (Form)sender;
            if (ColorTheme.Current.IsDark)
                ImmersiveDarkMode.UseImmersiveDarkMode(form, true);
        }

        public static void StylizeControl(Control control, bool designMode, StylizerOptions options = StylizerOptions.None, Func<Control, bool> shouldStylize = null)
        {
            StylizeControlInternal(control, designMode, options, shouldStylize);
        }

        private static StylizeControlResults StylizeControlInternal(Control control, bool designMode, StylizerOptions options,
            Func<Control, bool> shouldStylize = null)
        {
            if (designMode || (!options.HasFlag(StylizerOptions.StylizeTitleBar) &&  control is TitleBarControl))
                return new StylizeControlResults(false);

            bool loadRequired = control is CRichTextBox;
            if (shouldStylize == null || shouldStylize(control))
            {
                if (control is DataGridView grid)
                    StyleGrid(grid);
                else if (control is FormButton formButton)
                    StyleFormButton(formButton);
                else if (control is Button button)
                    StyleButton(button);
                else if (control is TextBox textBox)
                    StyleTextBox(textBox);
                else if (control is CRichTextBox richTextBox)
                    StyleRichTextBox(richTextBox);
                else if (control is LinkLabel linkLabel)
                    StyleLinkLabel(linkLabel);
                else if (control is ComboBox comboBox)
                    StyleCombo(comboBox);
                else if (control is CheckBox checkBox)
                    StyleCheckBox(checkBox);
                else if (control is CheckedListBox checkedListBox)
                    StyleCheckdListBox(checkedListBox);
                else if (control is ContextMenuStrip cms)
                    StyleContextMenuStrip(cms);
                else if (control is GroupBox groupBox)
                    StyleGroupBox(groupBox);
                else if (control is SlideShowPictureBox pb)
                    StylePictureBox(pb);
                else
                    StyleDefault(control);
            }

            foreach (Control subControl in control.Controls)
            {
                var results = StylizeControlInternal(subControl, designMode, options);
                loadRequired = loadRequired || results.LoadRequired;
            }

            return new StylizeControlResults(loadRequired);
        }

        private static void StylePictureBox(SlideShowPictureBox pb)
        {
            pb.BackColor = CurrentTheme.ImageBackground;
        }

        private static void StyleTextBox(TextBox textBox)
        {
            textBox.BackColor = CurrentTheme.TextBoxBackground;
            textBox.ForeColor = CurrentTheme.Text;
            textBox.BorderStyle = BorderStyle.FixedSingle;
        }

        private static void StyleRichTextBox(CRichTextBox textBox)
        {
            textBox.BackColor = CurrentTheme.TextBoxBackground;
            textBox.ForeColor = CurrentTheme.Text;
        }

        private static void StyleGroupBox(GroupBox groupBox)
        {
            groupBox.Paint += GroupBox_Paint;
        }

        private static void GroupBox_Paint(object sender, PaintEventArgs e)
        {
            GroupBox box = (GroupBox)sender;
            Graphics g = e.Graphics;
            e.Graphics.Clear(SystemColors.Control);
            e.Graphics.DrawString(box.Text, box.Font, Brushes.Black, 0, 0);

            Brush textBrush = new SolidBrush(CurrentTheme.Text);
            Brush borderBrush = new SolidBrush(CurrentTheme.Border);
            Pen borderPen = new Pen(borderBrush);
            SizeF strSize = g.MeasureString(box.Text, box.Font);
            Rectangle rect = new Rectangle(box.ClientRectangle.X,
                                           box.ClientRectangle.Y + (int)(strSize.Height / 2),
                                           box.ClientRectangle.Width - 1,
                                           box.ClientRectangle.Height - (int)(strSize.Height / 2) - 1);

            
            g.Clear(box.BackColor);
            g.DrawString(box.Text, box.Font, textBrush, box.Padding.Left, 0);

            g.DrawLine(borderPen, rect.Location, new Point(rect.X, rect.Y + rect.Height));
            g.DrawLine(borderPen, new Point(rect.X + rect.Width, rect.Y), new Point(rect.X + rect.Width, rect.Y + rect.Height));
            g.DrawLine(borderPen, new Point(rect.X, rect.Y + rect.Height), new Point(rect.X + rect.Width, rect.Y + rect.Height));
            g.DrawLine(borderPen, new Point(rect.X, rect.Y), new Point(rect.X + box.Padding.Left, rect.Y));
            g.DrawLine(borderPen, new Point(rect.X + box.Padding.Left + (int)(strSize.Width), rect.Y), new Point(rect.X + rect.Width, rect.Y));
        }

        private static void StyleDefault(Control control)
        {
            control.BackColor = CurrentTheme.Window;
            control.ForeColor = CurrentTheme.Text;
        }

        public static void StylizeControl(ToolStripDropDownButton control, bool designMode)
        {
            if (designMode)
                return;

            control.BackColor = CurrentTheme.Window;
            control.ForeColor = CurrentTheme.Text;

            foreach (var item in control.DropDownItems)
            {
                if (item is ToolStripMenuItem menuItem)
                {
                    menuItem.BackColor = CurrentTheme.Window;
                    menuItem.ForeColor = CurrentTheme.Text;
                    menuItem.Paint += MenuItem_Paint;
                    StyleMenuDropDownItems(menuItem);
                    continue;
                }

                if (item is ToolStripSeparator toolStripSeparator)
                    toolStripSeparator.Paint += ToolStripSeparator_Paint;
            }
        }

        private static void MenuItem_Paint(object sender, PaintEventArgs e)
        {
            if (sender is ToolStripMenuItem item)
            {
                PointF point = new PointF(24, 2);
                Rectangle rectangle = new Rectangle(2, 0, item.Size.Width - 4, item.Size.Height);
                if (item.Selected)
                {
                    e.Graphics.FillRectangle(new SolidBrush(ColorTheme.Current.Highlight), rectangle);
                    e.Graphics.DrawString(item.Text, item.Font, new SolidBrush(ColorTheme.Current.HighlightText), point);
                    return;
                }

                e.Graphics.FillRectangle(new SolidBrush(ColorTheme.Current.Window), rectangle);
                e.Graphics.DrawString(item.Text, item.Font, new SolidBrush(ColorTheme.Current.Text), point);
            }
        }

        private static void StyleContextMenuStrip(ContextMenuStrip cms)
        {
            cms.Renderer = new CToolStripRenderer();

            cms.BackColor = CurrentTheme.Window;
            cms.ForeColor = CurrentTheme.Text;            

            cms.ShowCheckMargin = false;
            cms.ShowImageMargin = false;

            foreach (var item in cms.Items)
            {
                if (!(item is ToolStripMenuItem menuItem))
                    continue;

                menuItem.BackColor = CurrentTheme.Window;
                menuItem.ForeColor = CurrentTheme.Text;
                StyleMenuDropDownItems(menuItem);
            }
        }

        private static void StyleMenuDropDownItems(ToolStripMenuItem menuItem)
        {
            foreach (var subItem in menuItem.DropDownItems)
            {
                if (subItem is ToolStripSeparator separator)
                {
                    separator.Paint += ToolStripSeparator_Paint;
                    continue;
                }

                if (!(subItem is ToolStripMenuItem subMenuItem))
                    continue;

                subMenuItem.BackColor = CurrentTheme.Window;
                subMenuItem.ForeColor = CurrentTheme.Text;
            }
        }

        private static void StyleCheckdListBox(CheckedListBox checkedListBox)
        {
            checkedListBox.BorderStyle = BorderStyle.None;
        }

        private static void StyleLinkLabel(LinkLabel label)
        {
            label.LinkColor = CurrentTheme.LinkText;
        }

        private static void StyleButton(Button button)
        {
            button.FlatStyle = CurrentTheme.ButtonFlatStyle;
            if (button.FlatStyle == FlatStyle.Flat)
                button.FlatAppearance.BorderSize = 0;
            button.ForeColor = CurrentTheme.ButtonTextColor;
            button.BackColor = CurrentTheme.ButtonBackground;
        }

        private static void StyleFormButton(FormButton button)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.FlatAppearance.BorderSize = 0;
            button.BackColor = CurrentTheme.Window;
        }

        private static void StyleCheckBox(CheckBox checkBox)
        {
            checkBox.ForeColor = CurrentTheme.Text;
            checkBox.BackColor = CurrentTheme.CheckBoxBackground;
            checkBox.FlatStyle = FlatStyle.Flat;
            checkBox.TextAlign = ContentAlignment.MiddleRight;
            checkBox.FlatAppearance.BorderSize = 0;
            checkBox.FlatAppearance.MouseOverBackColor = CurrentTheme.Window;
            checkBox.FlatAppearance.CheckedBackColor = CurrentTheme.Window;
            checkBox.FlatAppearance.MouseDownBackColor = CurrentTheme.Window;
            checkBox.AutoSize = true;
        }

        private static void StyleCombo(ComboBox comboBox)
        {
            comboBox.BackColor = CurrentTheme.TextBoxBackground;
            comboBox.ForeColor = CurrentTheme.Text;
            comboBox.DrawMode = DrawMode.OwnerDrawFixed;
            comboBox.FlatStyle = CurrentTheme.ComboFlatStyle;

            // CComboBox implements it's own DrawItem
            if (comboBox is CComboBox)
                return;

            comboBox.DrawItem += CComboBox.ComboBoxDrawItem;
        }

        private static void StyleGrid(DataGridView view)
        {
            view.DefaultCellStyle.NullValue = "N/A";
            view.RowHeadersVisible = false;
            view.AutoGenerateColumns = false;
            view.ShowCellToolTips = false;
            view.EnableHeadersVisualStyles = false;

            view.ColumnHeadersDefaultCellStyle.BackColor = CurrentTheme.Window;
            view.ColumnHeadersDefaultCellStyle.ForeColor = CurrentTheme.Text;
            view.ColumnHeadersDefaultCellStyle.SelectionBackColor = view.ColumnHeadersDefaultCellStyle.BackColor;
            view.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            view.ColumnHeadersDefaultCellStyle.Padding = new Padding(0, 4, 0, 4);

            view.ForeColor = CurrentTheme.Text;
            view.BackgroundColor = CurrentTheme.GridBackground;
            view.RowsDefaultCellStyle.ForeColor = CurrentTheme.Text;
            view.RowsDefaultCellStyle.BackColor = CurrentTheme.GridRow;
            view.AlternatingRowsDefaultCellStyle.ForeColor = CurrentTheme.Text;
            view.AlternatingRowsDefaultCellStyle.BackColor = CurrentTheme.GridRowAlt;
            view.DefaultCellStyle.SelectionBackColor = CurrentTheme.Highlight;

            view.BorderStyle = BorderStyle.None;
            view.Paint += View_Paint;

            if (!CurrentTheme.GridRowBorder)
                view.CellPainting += View_CellPainting;
        }

        private static void View_Paint(object sender, PaintEventArgs e)
        {
            DataGridView view = sender as DataGridView;
            Rectangle rect = new Rectangle(view.ClientRectangle.Location, new Size(view.ClientRectangle.Width -1, view.ClientRectangle.Height - 1));
            e.Graphics.DrawRectangle(new Pen(CurrentTheme.GridBorder), rect);
        }

        private static void View_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            e.AdvancedBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
            e.AdvancedBorderStyle.Bottom = DataGridViewAdvancedCellBorderStyle.None;
            e.AdvancedBorderStyle.Left = DataGridViewAdvancedCellBorderStyle.None;
            e.AdvancedBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
        }

        private static void ToolStripSeparator_Paint(object sender, PaintEventArgs e)
        {
            ToolStripSeparator toolStripSeparator = (ToolStripSeparator)sender;
            int width = toolStripSeparator.Width;
            int height = toolStripSeparator.Height;

            Color foreColor = CurrentTheme.Separator;
            Color backColor = CurrentTheme.Window;

            DpiScale dpiScale = new DpiScale(e.Graphics);
            int pad = dpiScale.ScaleIntX(8);
            e.Graphics.FillRectangle(new SolidBrush(backColor), 0, 0, width, height);
            e.Graphics.DrawLine(new Pen(foreColor), pad, height / 2, width, height / 2);
        }
    }
}
