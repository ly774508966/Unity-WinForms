﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Drawing;

namespace System.Windows.Forms
{
    public abstract class ToolStripItem : Component
    {
        private bool _hovered;

        internal bool JustVisual { get; set; }

        public virtual Color BackColor { get; set; }
        public virtual Rectangle Bounds { get { return new Rectangle(0, 0, Width, Height); } }
        public virtual bool Enabled { get; set; }
        public virtual Font Font { get; set; }
        public virtual Color ForeColor { get; set; }
        public int Height { get; set; }
        public bool Hovered { get { return _hovered; } }
        public Color HoverColor { get; set; }
        public Rectangle HoverPadding { get; set; }
        public virtual Bitmap Image { get; set; }
        public Color ImageColor { get; set; }
        public string Name { get; set; }
        public ToolStrip Owner { get; set; }
        public ToolStripItem OwnerItem { get; internal set; }
        public virtual Padding Padding { get; set; }
        protected internal ToolStrip Parent { get; set; }
        public virtual bool Pressed { get { return false; } }
        public virtual bool Selected { get; internal set; }
        public Size Size { get { return new Size(Width, Height); } set { Width = value.Width; Height = value.Height; } }
        public virtual string Text { get; set; }
        public virtual ContentAlignment TextAlign { get; set; }
        public bool Visible { get; set; }
        public int Width { get; set; }

        public event EventHandler Click = delegate { };

        protected ToolStripItem()
        {
            Enabled = true;
            ForeColor = Color.FromArgb(64, 64, 64);
            HoverColor = Color.FromArgb(64, 200, 200, 200);
            HoverPadding = new Rectangle(2, 2, -4, -4);
            ImageColor = Color.White;
            Name = "toolStripItem";
            Padding = new Forms.Padding(8, 0, 8, 0);
            Size = new Drawing.Size(160, 24);
            TextAlign = ContentAlignment.MiddleLeft;
        }
        protected ToolStripItem(string text) : this()
        {
            Text = text;
        }

        protected virtual void OnMouseDown(MouseEventArgs e)
        {

        }
        protected virtual void OnMouseEnter(EventArgs e)
        {
            _hovered = true;
        }
        protected virtual void OnMouseHover(EventArgs e)
        {
            
        }
        protected virtual void OnMouseLeave(EventArgs e)
        {
            _hovered = false;
        }
        protected virtual void OnMouseMove(MouseEventArgs mea)
        {

        }
        protected virtual void OnMouseUp(MouseEventArgs e)
        {
            if (!Enabled) return;
            Click(this, null);
            if (Parent.Parent == null)
                Parent.Dispose();
        }
        protected virtual void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;

            var ex = e.ClipRectangle.X;
            var ey = e.ClipRectangle.Y;
            var ew = e.ClipRectangle.Width;
            var eh = e.ClipRectangle.Height;

            g.FillRectangle(BackColor, ex, ey, ew, eh);
            if (Image != null)
                g.DrawTexture(Image, ex + 6, ey + 4, 12, 12, ImageColor);
            if (Enabled)
            {
                if (!Selected)
                {
                    if (_hovered)
                    {
                        g.FillRectangle(HoverColor, e.ClipRectangle + HoverPadding);
                        g.DrawRectangle(new Pen(HoverColor - Color.FromArgb(0, 64, 64, 64)), e.ClipRectangle + HoverPadding);
                    }
                }
                else
                {
                    if (Owner.Orientation == Orientation.Horizontal)
                        g.FillRectangle(Color.FromArgb(246, 246, 246), e.ClipRectangle);
                    else
                    {
                        g.FillRectangle(HoverColor, e.ClipRectangle + HoverPadding);
                        g.DrawRectangle(new Pen(HoverColor - Color.FromArgb(0, 64, 64, 64)), e.ClipRectangle + HoverPadding);
                    }
                }
            }

            Color textColor = Enabled ? ForeColor : ForeColor + Color.FromArgb(0, 100, 100, 100);
            if (Selected && Enabled) textColor = Color.FromArgb(64, 64, 64);

            if (Parent.Orientation == Orientation.Horizontal)
                g.DrawString(Text, Font, textColor, ex, ey, ew, eh, TextAlign);
            else
                g.DrawString(Text, Font, textColor, ex + 32, ey, ew, eh, TextAlign);
            if (!Selected)
                g.DrawRectangle(new Pen(BackColor), e.ClipRectangle);
        }

        internal void RaiseClick()
        {
            Click(this, null);
        }
        internal void RaiseOnMouseDown(MouseEventArgs e)
        {
            OnMouseDown(e);
        }
        internal void RaiseOnMouseEnter(EventArgs e)
        {
            OnMouseEnter(e);
        }
        internal void RaiseOnMouseHover(EventArgs e)
        {
            OnMouseHover(e);
        }
        internal void RaiseOnMouseLeave(EventArgs e)
        {
            OnMouseLeave(e);
        }
        internal void RaiseOnMouseMove(MouseEventArgs mea)
        {
            OnMouseMove(mea);
        }
        internal void RaiseOnMouseUp(MouseEventArgs e)
        {
            OnMouseUp(e);
        }
        internal void RaiseOnPaint(PaintEventArgs e)
        {
            OnPaint(e);
        }
        internal void ResetSelected()
        {
            if (Parent.UWF_Context && Owner.OwnerItem != null)
            {
                var parent = Owner.OwnerItem.Owner;
                while (true)
                {
                    if (parent == null) break;

                    if (!parent.UWF_Context)
                    {
                        parent.ResetSelected();
                        break;
                    }

                    if (parent.OwnerItem != null)
                        parent = parent.OwnerItem.Owner;
                    else
                        break;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {

        }
    }

    public class ToolStripButton : ToolStripItem
    {

    }
    public class ToolStripDropDownItem : ToolStripItem
    {
        private readonly ToolStripItemCollection _dropDownItems;
        private ToolStrip _dropDownToolStrip;
        private bool _pressed;

        protected ToolStripDropDownItem()
        {
            _dropDownItems = new ToolStripItemCollection(Parent, null);

            ArrowColor = Color.Black;
            ArrowImage = ApplicationBehaviour.Resources.Images.DropDownRightArrow;
        }

        public Color ArrowColor { get; set; }
        public Bitmap ArrowImage { get; set; }
        public ToolStripItemCollection DropDownItems { get { return _dropDownItems; } }
        public override bool Pressed
        {
            get
            {
                return _pressed;
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            if (!Enabled) return;

            RaiseClick();
            if (DropDownItems.Count == 0)
            {
                ResetSelected();
                if (Parent.Parent == null)
                    Parent.Dispose();
                return;
            }

            if (_dropDownToolStrip != null)
            {
                _dropDownToolStrip = null;
                return;
            }

            if (!_pressed)
            {
                _dropDownToolStrip = new ToolStrip();
                _dropDownToolStrip.UWF_Context = true;
                _dropDownToolStrip.OwnerItem = this;

                if (Parent.UWF_ShadowHandler != null)
                    _dropDownToolStrip.MakeShadow();

                int index = Parent.Items.IndexOf(this);
                int x = 0, y = 0;
                for (int i = 0; i < index; i++)
                {
                    if (Parent.Orientation == Forms.Orientation.Horizontal)
                        x += Parent.Items[i].Width;
                    if (Parent.Orientation == Forms.Orientation.Vertical)
                        y += Parent.Items[i].Height;
                }
                //_dropDownToolStrip.BackColor = Parent.BackColor;
                _dropDownToolStrip.Items.AddRange(DropDownItems);
                for (int i = 0; i < _dropDownToolStrip.Items.Count; i++)
                {
                    _dropDownToolStrip.Items[i].OwnerItem = this;
                    _dropDownToolStrip.Items[i].Selected = false;
                }
                _dropDownToolStrip.UWF_ShadowBox = true;
                _dropDownToolStrip.Orientation = Orientation.Vertical;
                int height = 0;
                for (int i = 0; i < DropDownItems.Count; i++)
                    height += DropDownItems[i].Height;
                _dropDownToolStrip.Size = new Size(DropDownItems[0].Width, height);

                var parentLocationClient = Parent.PointToScreen(Point.Zero);
                if (Parent.Orientation == Orientation.Horizontal)
                {
                    _dropDownToolStrip.Location = new Point(parentLocationClient.X + x + Parent.Padding.Left, parentLocationClient.Y + Parent.Height - HoverPadding.Y - 1);
                    _dropDownToolStrip.BorderColor = Color.Transparent;
                }
                else
                {
                    _dropDownToolStrip.Location = new Point(parentLocationClient.X + x + Parent.Width, parentLocationClient.Y + y);
                    _dropDownToolStrip.BorderColor = Parent.BorderColor;

                    if (_dropDownToolStrip.Location.X + _dropDownToolStrip.Width > Screen.PrimaryScreen.WorkingArea.Width)
                    {
                        _dropDownToolStrip.Location = new Point(parentLocationClient.X - _dropDownToolStrip.Width, _dropDownToolStrip.Location.Y);
                    }
                }

                _dropDownToolStrip.OnDisposing += (object sender, EventArgs args) =>
                {
                    var clientRect = new System.Drawing.Rectangle(x, y, Width, Height);
                    var contains = clientRect.Contains(Parent.PointToClient(Control.MousePosition));
                    if (!contains)
                        _pressed = false;
                    else
                        _pressed = !_pressed;
                    _dropDownToolStrip = null;
                };
            }
            else
            {
                _pressed = false;
            }
        }
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            if (_dropDownItems.Count > 0 && Parent.Orientation == Orientation.Vertical)
            {
                e.Graphics.DrawTexture(ArrowImage, e.ClipRectangle.X + e.ClipRectangle.Width - 26, e.ClipRectangle.Y, 24, 24, ArrowColor);
            }
        }
    }
    public class ToolStripLabel : ToolStripItem
    {

    }
    public class ToolStripSeparator : ToolStripItem
    {
        protected Pen borderColor = new Pen(Color.FromArgb(215, 215, 215));
        protected Pen borderColor2 = new Pen(Color.White);

        public ToolStripSeparator()
        {
            JustVisual = true;
            Height = 0;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            var ex = e.ClipRectangle.X + 32;
            var ey = e.ClipRectangle.Y;
            var ex2 = ex + e.ClipRectangle.Width - 8;
            var ey2 = e.ClipRectangle.Y + 1;

            e.Graphics.DrawLine(borderColor, ex, ey, ex2, ey);
            e.Graphics.DrawLine(borderColor2, ex, ey2, ex2, ey2);
        }
    }

    public class ToolStripMenuItem : ToolStripDropDownItem
    {
        private StringFormat _shortcutKeysFormat = new StringFormat() { LineAlignment = StringAlignment.Center };

        public bool Checked { get; set; }
        public string ShortcutKeys { get; set; } // TODO: enum (flag).

        public ToolStripMenuItem()
        {
        }
        public ToolStripMenuItem(string text)
        {
            Text = text;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Checked)
            {
                int rectWH = e.ClipRectangle.Height - 8;
                int checkedWH = e.ClipRectangle.Height - 12;

                e.Graphics.FillRectangle(HoverColor, e.ClipRectangle.X + 4, e.ClipRectangle.Y + 4, rectWH, rectWH);
                e.Graphics.DrawRectangle(new Pen(HoverColor - Color.FromArgb(0, 64, 64, 64)), e.ClipRectangle.X + 4, e.ClipRectangle.Y + 4, rectWH, rectWH);
                e.Graphics.DrawTexture(ApplicationBehaviour.Resources.Images.Checked, e.ClipRectangle.X + 6, e.ClipRectangle.Y + 6, checkedWH, checkedWH);
            }

            if (!String.IsNullOrEmpty(ShortcutKeys))
            {
                //e.Graphics.DrawRectangle(Pens.DarkRed, e.ClipRectangle);
                if (Parent.Orientation == Orientation.Vertical)
                    e.Graphics.DrawString(ShortcutKeys, Font,
                        new SolidBrush(Enabled ? ForeColor : ForeColor + Color.FromArgb(0, 100, 100, 100)),
                        e.ClipRectangle.X + e.ClipRectangle.Width - 60, e.ClipRectangle.Y, 60, e.ClipRectangle.Height, _shortcutKeysFormat);
            }
        }
    }
}
