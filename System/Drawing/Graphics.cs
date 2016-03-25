﻿using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace System.Drawing
{
    public sealed class Graphics : IDeviceContext, IDisposable
    {
        private bool _group { get { return _groupControls.Count > 0; } }
        private Control _groupControlLast { get { return _groupControls[_groupControls.Count - 1]; } }
        private List<Control> _groupControls = new List<Control>();

        public static UnityEngine.Material DefaultMaterial { get; set; }
        public static bool GL_Lines { get; set; }
        public static bool NoFill { get; set; }
        public static bool NoRects { get; set; }
        public static bool NoStrings { get; set; }

        internal Control Control { get; set; }
        internal Rectangle Group { get; set; }
        internal void GroupBegin(Control groupControl)
        {
            var c_position = Control.Location + Control.Offset;//Control.PointToScreen(Point.Zero);
            GUI.BeginGroup(new Rect((c_position.X + Group.X), (c_position.Y + Group.Y), Group.Width, Group.Height));
            _groupControls.Add(groupControl);
        }
        internal void GroupEnd()
        {
            GUI.EndGroup();
            _groupControls.RemoveAt(_groupControls.Count - 1);
        }

        private PointF[] GetBezierApproximation(PointF[] controlPoints, int outputSegmentCount)
        {
            PointF[] points = new PointF[outputSegmentCount + 1];
            for (int i = 0; i <= outputSegmentCount; i++)
            {
                float t = (float)i / outputSegmentCount;
                points[i] = GetBezierPoint(t, controlPoints, 0, controlPoints.Length);
            }
            return points;
        }
        private PointF GetBezierPoint(float t, PointF[] controlPoints, int index, int count)
        {
            if (count == 1)
                return controlPoints[index];
            var P0 = GetBezierPoint(t, controlPoints, index, count - 1);
            var P1 = GetBezierPoint(t, controlPoints, index + 1, count - 1);
            return new PointF((1 - t) * P0.X + t * P1.X, (1 - t) * P0.Y + t * P1.Y);
        }
        private static int GUI_SetFont(Font font)
        {
            int guiSkinFontSizeBuffer = GUI.skin.label.fontSize;
            if (font != null)
            {
                var _font = System.Windows.Forms.Application.Resources.Fonts.Find(f => f.fontNames[0] == font.Name);
                if (_font != null)
                    GUI.skin.label.font = _font;
                else
                {
                    GUI.skin.label.font = null;
                    UnityEngine.Debug.LogWarning(font.Name);
                }
                GUI.skin.label.fontSize = (int)(font.Size);
                bool styleBold = (font.Style & FontStyle.Bold) == FontStyle.Bold;
                bool styleItalic = (font.Style & FontStyle.Italic) == FontStyle.Italic;
                if (styleBold)
                {
                    if (styleItalic)
                        GUI.skin.label.fontStyle = UnityEngine.FontStyle.BoldAndItalic;
                    else
                        GUI.skin.label.fontStyle = UnityEngine.FontStyle.Bold;
                }
                else if (styleItalic)
                    GUI.skin.label.fontStyle = UnityEngine.FontStyle.Italic;
                else GUI.skin.label.fontStyle = UnityEngine.FontStyle.Normal;
            }
            else
            {
                var _font = System.Windows.Forms.Application.Resources.Fonts.Find(f => f.fontNames[0] == "Arial");
                if (_font != null)
                    GUI.skin.label.font = _font;
                GUI.skin.label.fontSize = (int)(12);
                GUI.skin.label.fontStyle = UnityEngine.FontStyle.Normal;
            }
            return guiSkinFontSizeBuffer;
        }

        public void Dispose()
        {

        }

        public void Clear(System.Drawing.Color color)
        {
        }
        public void DrawCurve(Pen pen, PointF[] points) // very slow.
        {
            if (points == null || points.Length <= 1) return;
            if (points.Length == 2)
            {
                DrawLine(pen, points[0].X, points[0].Y, points[1].X, points[1].Y);
                return;
            }

            var bPoints = GetBezierApproximation(points, 32); // decrease segments for better fps.
            for (int i = 0; i + 1 < bPoints.Length; i++)
                DrawLine(pen, bPoints[i].X, bPoints[i].Y, bPoints[i + 1].X, bPoints[i + 1].Y);
        }
        public void DrawImage(Image image, float x, float y, float width, float height)
        {
            DrawTexture(image.uTexture, x, y, width, height, image.Color);
        }
        public void DrawLine(Pen pen, int x1, int y1, int x2, int y2)
        {
            DrawLine(pen, (float)x1, (float)y1, (float)x2, (float)y2);
        }
        public void DrawLine(Pen pen, float x1, float y1, float x2, float y2)
        {
            if (GL_Lines)
            {
                GL.Begin(GL.LINES);
                GL.Color(pen.Color.ToUColor());

                // TODO: switch (pen.DashStyle) { ... }

                GL.Vertex3(x1, y1, 0);
                GL.Vertex3(x2, y2, 0);

                GL.End();
                return;
            }

            float x = 0;
            float y = 0;
            float width = 0;
            float height = 0;

            if (x1 != x2 && y1 != y2)
            {
                float xDiff = x2 - x1;
                float yDiff = y2 - y1;
                var angle = Math.Atan2(yDiff, xDiff) * 180.0 / Math.PI;

                DrawTexture(System.Windows.Forms.Application.DefaultSpriteSmoothLine, x1, y1, (float)Math.Sqrt(xDiff * xDiff + yDiff * yDiff), pen.Width, pen.Color, (float)angle, new PointF());
                return;
            }

            if (x1 == x2)
            {
                if (y1 > y2) y1 += pen.Width;
                else y2 += pen.Width;
                x = x1;
                width = pen.Width;
                if (y1 < y2)
                {
                    y = y1;
                    height = y2 - y1;
                }
                else
                {
                    y = y2;
                    height = y1 - y2;
                }
            }
            if (y1 == y2)
            {
                y = y1;
                height = pen.Width;
                if (x1 < x2)
                {
                    x = x1;
                    width = x2 - x1;
                }
                else
                {
                    x = x2;
                    width = x1 - x2;
                }
            }

            GUI.color = pen.Color.ToUColor();
            if (!_group)
            {
                Point c_position = Point.Empty;
                if (Control != null)
                    c_position = Control.PointToScreen(Point.Zero);

                GUI.DrawTexture(new Rect(c_position.X + x, c_position.Y + y, width, height), System.Windows.Forms.Application.DefaultSprite);
            }
            else
            {
                Point c_position = Point.Empty;
                if (Control != null)
                    c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);
                var position = c_position - g_position + new PointF(x, y);

                switch (pen.DashStyle)
                {
                    case Drawing2D.DashStyle.Solid:
                        GUI.DrawTexture(new Rect(position.X, position.Y, width, height), System.Windows.Forms.Application.DefaultSprite);
                        break;
                    case Drawing2D.DashStyle.Dash:
                        float dash_step = pen.Width * 6;
                        if (y1 == y2)
                            for (float i = 0; i < width; i += dash_step)
                            {
                                float dash_width = dash_step - 2;
                                if (i + dash_width > width)
                                    dash_width = width - i;
                                GUI.DrawTexture(new Rect(position.X + i, position.Y, dash_width, pen.Width), System.Windows.Forms.Application.DefaultSprite);
                            }

                        if (x1 == x2)
                            for (float i = 0; i < height; i += dash_step)
                            {
                                float dash_height = dash_step - 2;
                                if (i + dash_height > height)
                                    dash_height = height - i;
                                GUI.DrawTexture(new Rect(position.X + width - pen.Width, position.Y + i, pen.Width, dash_height), System.Windows.Forms.Application.DefaultSprite);
                            }
                        break;
                }
            }

        }
        public void DrawMesh(Mesh mesh, Point position, Quaternion rotation, Material mat)
        {
            mat.SetPass(0);
            UnityEngine.Graphics.DrawMeshNow(mesh, new Vector3(0, 0, 0), rotation);
        }
        public string DrawPasswordField(string s, Font font, SolidBrush brush, float x, float y, float width, float height, HorizontalAlignment alignment)
        {
            if (Control == null) return s;

            GUI.skin.textField.alignment = TextAnchor.UpperLeft;
            switch (alignment)
            {
                case HorizontalAlignment.Center:
                    GUI.skin.textField.alignment = TextAnchor.MiddleCenter;
                    break;
                default:
                    GUI.skin.textField.alignment = TextAnchor.MiddleLeft;
                    break;
                case HorizontalAlignment.Right:
                    GUI.skin.textField.alignment = TextAnchor.MiddleRight;
                    break;
            }

            if (font != null)
            {
                var _font = System.Windows.Forms.Application.Resources.Fonts.Find(f => f.fontNames[0] == font.Name);
                if (_font != null)
                    GUI.skin.textField.font = _font;
                else
                    GUI.skin.textField.font = null;
                GUI.skin.textField.fontSize = (int)font.Size;
                bool styleBold = (font.Style & FontStyle.Bold) == FontStyle.Bold;
                bool styleItalic = (font.Style & FontStyle.Italic) == FontStyle.Italic;
                if (styleBold)
                {
                    if (styleItalic)
                        GUI.skin.textField.fontStyle = UnityEngine.FontStyle.BoldAndItalic;
                    else
                        GUI.skin.textField.fontStyle = UnityEngine.FontStyle.Bold;
                }
                else if (styleItalic)
                    GUI.skin.textField.fontStyle = UnityEngine.FontStyle.Italic;
                else GUI.skin.textField.fontStyle = UnityEngine.FontStyle.Normal;
            }
            else
            {
                var _font = System.Windows.Forms.Application.Resources.Fonts.Find(f => f.fontNames[0] == "Arial");
                if (_font != null)
                    GUI.skin.textField.font = _font;
                GUI.skin.textField.fontSize = 12;
            }

            GUI.color = brush.Color.ToUColor();

            if (!_group)
            {
                var c_position = Control.PointToScreen(Point.Zero);
                return GUI.PasswordField(new Rect(c_position.X + x, c_position.Y + y, width, height), s, '*');
            }
            else
            {
                var c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);
                var position = c_position - g_position + new PointF(x, y);

                return GUI.PasswordField(new Rect(position.X, position.Y, width, height), s, '*');
            }
        }
        public void DrawPoint(Color color, Point point)
        {
            DrawPoint(color, point.X, point.Y);
        }
        public void DrawPoint(Color color, PointF point)
        {
            DrawPoint(color, point.X, point.Y);
        }
        public void DrawPoint(Color color, int x, int y)
        {
            DrawPoint(color, x, y);
        }
        public void DrawPoint(Color color, float x, float y)
        {
            DrawTexture(System.Windows.Forms.Application.DefaultSprite, x, y, 1, 1);
        }
        public void DrawPolygon(Pen pen, Point[] points)
        {
            if (DefaultMaterial != null)
                DefaultMaterial.SetPass(0);

            for (int i = 0; i < points.Length; i++)
            {
                if (i + 1 >= points.Length) break;

                GL.Begin(GL.LINES);
                GL.Color(pen.Color.ToUColor());

                GL.Vertex3(points[i].X, points[i].Y, 0);
                GL.Vertex3(points[i + 1].X, points[i + 1].Y, 0);

                GL.End();
            }
        }
        public void DrawRectangle(Pen pen, Rectangle rect)
        {
            DrawRectangle(pen, rect.X, rect.Y, rect.Width, rect.Height);
        }
        public void DrawRectangle(Pen pen, int x, int y, int width, int height)
        {
            DrawRectangle(pen, (float)x, (float)y, (float)width, (float)height);
        }
        public void DrawRectangle(Pen pen, float x, float y, float width, float height)
        {
            if (NoRects) return;
            if (pen.Color == Color.Transparent) return;
            GUI.color = pen.Color.ToUColor();

            /*if (Control.Batched)
            {
                GUI.color = UnityEngine.Color.white;
                GUI.DrawTexture(new Rect(x, y, width, height), Control.BatchedTexture);
                return;
            }*/

            if (!_group)
            {
                Point c_position = Point.Empty;
                if (Control != null)
                    c_position = Control.PointToScreen(Point.Zero);
                // Top.
                GUI.DrawTexture(new Rect(c_position.X + x, c_position.Y + y, width, pen.Width), System.Windows.Forms.Application.DefaultSprite);
                // Right.
                GUI.DrawTexture(new Rect(c_position.X + x + width - pen.Width, c_position.Y + y + pen.Width, pen.Width, height - pen.Width * 2), System.Windows.Forms.Application.DefaultSprite);
                // Bottom.
                if (height > 1)
                    GUI.DrawTexture(new Rect(c_position.X + x, c_position.Y + y + height - pen.Width, width, pen.Width), System.Windows.Forms.Application.DefaultSprite);
                // Left.
                if (width > 1)
                    GUI.DrawTexture(new Rect(c_position.X + x, c_position.Y + y + pen.Width, pen.Width, height - pen.Width * 2), System.Windows.Forms.Application.DefaultSprite);
            }
            else
            {
                Point c_position = Point.Empty;
                if (Control != null)
                    c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);
                var position = c_position - g_position + new PointF(x, y);

                switch (pen.DashStyle)
                {
                    case Drawing2D.DashStyle.Solid:
                        /*if (width == 0 || height == 0) return;
                        if (width < 0 || height < 0)
                        {
                            if (width < 0)
                            {
                                x += width;
                                width *= -1;
                            }
                            if (height < 0)
                            {
                                y += height;
                                height *= -1;
                            }
                        }


                        //UnityEngine.Debug.Log(width.ToString() + " " + height.ToString());
                        //return;

                        // Batching
                        UnityEngine.Texture2D tex = new Texture2D((int)width, (int)height);
                        for (int i = 0; i < tex.height; i++)
                            for (int k = 0; k < tex.width; k++)
                                tex.SetPixel(k, i, new UnityEngine.Color(1, 1, 1, 0));

                        var ucolor = pen.Color.ToUColor();
                        for (int i = 0; i < tex.width; i++)
                        {
                            for (int k = 0; k < pen.Width && k < tex.height; k++)
                                tex.SetPixel(i, k, ucolor);
                            for (int k = tex.height - 1; k > 0 && k > tex.height - 1 - pen.Width; k--)
                                tex.SetPixel(i, k, ucolor);
                        }

                        for (int i = 0; i < tex.height; i++)
                        {
                            for (int k = 0; k < pen.Width && k < tex.width; k++)
                                tex.SetPixel(k, i, ucolor);
                            for (int k = tex.width - 1; k > 0 && k > tex.width - 1 - pen.Width; k--)
                                tex.SetPixel(k, i, ucolor);
                        }

                        tex.Apply();
                        Control.BatchedTexture = tex;*/

                        GUI.DrawTexture(new Rect(position.X, position.Y, width, pen.Width), System.Windows.Forms.Application.DefaultSprite);
                        GUI.DrawTexture(new Rect(position.X + width - pen.Width, position.Y + pen.Width, pen.Width, height - pen.Width * 2), System.Windows.Forms.Application.DefaultSprite);
                        if (height > 1)
                            GUI.DrawTexture(new Rect(position.X, position.Y + height - pen.Width, width, pen.Width), System.Windows.Forms.Application.DefaultSprite);
                        if (width > 1)
                            GUI.DrawTexture(new Rect(position.X, position.Y + pen.Width, pen.Width, height - pen.Width * 2), System.Windows.Forms.Application.DefaultSprite);

                        break;
                    case Drawing2D.DashStyle.Dash:
                        float dash_step = pen.Width * 6;
                        for (float i = 0; i < width; i += dash_step)
                        {
                            float dash_width = dash_step - 2;
                            if (i + dash_width > width)
                                dash_width = width - i;
                            GUI.DrawTexture(new Rect(position.X + i, position.Y, dash_width, pen.Width), System.Windows.Forms.Application.DefaultSprite); // Top.
                            GUI.DrawTexture(new Rect(position.X + i, position.Y + height - pen.Width, dash_width, pen.Width), System.Windows.Forms.Application.DefaultSprite); // Bottom.
                        }
                        for (float i = 0; i < height; i += dash_step)
                        {
                            float dash_height = dash_step - 2;
                            if (i + dash_height > height)
                                dash_height = height - i;
                            GUI.DrawTexture(new Rect(position.X + width - pen.Width, position.Y + i, pen.Width, dash_height), System.Windows.Forms.Application.DefaultSprite); // Right.
                            GUI.DrawTexture(new Rect(position.X, position.Y + i, pen.Width, dash_height), System.Windows.Forms.Application.DefaultSprite); // Left.
                        }
                        break;
                }
            }
        }
        public void DrawString(string s, Font font, SolidBrush brush, PointF point)
        {
            DrawString(s, font, brush, point.X, point.Y);
        }
        public void DrawString(string s, Font font, SolidBrush brush, float x, float y)
        {
            DrawString(s, font, brush, x, y, 512, 64);
        }
        public void DrawString(string s, Font font, Color color, float x, float y)
        {
            DrawString(s, font, color, x, y, 512, 64);
        }
        public void DrawString(string s, Font font, SolidBrush brush, float x, float y, StringFormat format)
        {
            DrawString(s, font, brush, x, y, 512, 64, format);
        }
        public void DrawString(string s, Font font, SolidBrush brush, float x, float y, float width, float height)
        {
            DrawString(s, font, brush, x, y, width, height, new StringFormat());
        }
        public void DrawString(string s, Font font, Color color, float x, float y, float width, float height)
        {
            DrawString(s, font, color, x, y, width, height, ContentAlignment.BottomLeft);
        }
        public void DrawString(string s, Font font, SolidBrush brush, float x, float y, float width, float height, ContentAlignment alignment)
        {
            DrawString(s, font, brush.Color, x, y, width, height, alignment);
        }
        public void DrawString(string s, Font font, Color color, float x, float y, float width, float height, ContentAlignment alignment)
        {
            if (NoStrings) return;
            GUI.skin.label.alignment = TextAnchor.UpperLeft;
            switch (alignment)
            {
                case ContentAlignment.BottomCenter:
                    GUI.skin.label.alignment = TextAnchor.LowerCenter;
                    break;
                case ContentAlignment.BottomLeft:
                    GUI.skin.label.alignment = TextAnchor.LowerLeft;
                    break;
                case ContentAlignment.BottomRight:
                    GUI.skin.label.alignment = TextAnchor.LowerRight;
                    break;
                case ContentAlignment.MiddleCenter:
                    GUI.skin.label.alignment = TextAnchor.MiddleCenter;
                    break;
                case ContentAlignment.MiddleLeft:
                    GUI.skin.label.alignment = TextAnchor.MiddleLeft;
                    break;
                case ContentAlignment.MiddleRight:
                    GUI.skin.label.alignment = TextAnchor.MiddleRight;
                    break;
                case ContentAlignment.TopCenter:
                    GUI.skin.label.alignment = TextAnchor.UpperCenter;
                    break;
                case ContentAlignment.TopLeft:
                    GUI.skin.label.alignment = TextAnchor.UpperLeft;
                    break;
                case ContentAlignment.TopRight:
                    GUI.skin.label.alignment = TextAnchor.UpperRight;
                    break;
            }

            int guiSkinFontSizeBuffer = GUI_SetFont(font);
            GUI.color = color.ToUColor();

            if (!_group)
            {
                Point c_position = Point.Empty;
                if (Control != null)
                    c_position = Control.PointToScreen(Point.Zero);
                GUI.Label(new Rect(c_position.X + x, c_position.Y + y, width, height), s);
            }
            else
            {
                //Point c_position = Point.Empty;
                //if (Control != null)
                //  c_position = Control.PointToScreen(Point.Zero);
                //var g_position = _groupControlLast.PointToScreen(Point.Zero);
                //var position = c_position - g_position + new PointF(x, y);

                GUI.Label(new Rect(x, y, width, height), s);
            }

            GUI.skin.label.fontSize = guiSkinFontSizeBuffer;
        }
        public void DrawString(string s, Font font, SolidBrush brush, float x, float y, float width, float height, HorizontalAlignment horizontalAlignment)
        {
            ContentAlignment alignment = ContentAlignment.MiddleLeft;
            switch (horizontalAlignment)
            {
                default:
                    alignment = ContentAlignment.MiddleLeft;
                    break;
                case HorizontalAlignment.Center:
                    alignment = ContentAlignment.MiddleCenter;
                    break;
                case HorizontalAlignment.Right:
                    alignment = ContentAlignment.MiddleRight;
                    break;
            }
            DrawString(s, font, brush, x, y, width, height, alignment);
        }
        public void DrawString(string s, Font font, SolidBrush brush, float x, float y, float width, float height, StringFormat format)
        {
            ContentAlignment alignment = ContentAlignment.TopLeft;
            switch (format.Alignment)
            {
                case StringAlignment.Near:
                    if (format.LineAlignment == StringAlignment.Near)
                        alignment = ContentAlignment.TopLeft;
                    else if (format.LineAlignment == StringAlignment.Center)
                        alignment = ContentAlignment.MiddleLeft;
                    else alignment = ContentAlignment.BottomLeft;
                    break;
                case StringAlignment.Center:
                    if (format.LineAlignment == StringAlignment.Near)
                        alignment = ContentAlignment.TopCenter;
                    else if (format.LineAlignment == StringAlignment.Center)
                        alignment = ContentAlignment.MiddleCenter;
                    else alignment = ContentAlignment.BottomCenter;
                    break;
                case StringAlignment.Far:
                    if (format.LineAlignment == StringAlignment.Near)
                        alignment = ContentAlignment.TopRight;
                    else if (format.LineAlignment == StringAlignment.Center)
                        alignment = ContentAlignment.MiddleRight;
                    else alignment = ContentAlignment.BottomRight;
                    break;
            }
            DrawString(s, font, brush, x, y, width, height, alignment);
        }
        public void DrawString(string s, Font font, SolidBrush brush, RectangleF layoutRectangle)
        {
            DrawString(s, font, brush, layoutRectangle, new StringFormat());
        }
        public void DrawString(string s, Font font, SolidBrush brush, RectangleF layoutRectangle, StringFormat format)
        {
            DrawString(s, font, brush, layoutRectangle.X, layoutRectangle.Y, layoutRectangle.Width, layoutRectangle.Height, format);
        }
        public string DrawTextArea(string s, Font font, SolidBrush brush, float x, float y, float width, float height)
        {
            if (Control == null) return s;

            GUI.skin.textArea.alignment = TextAnchor.UpperLeft;

            GUI.color = brush.Color.ToUColor();
            //GUI.skin.textArea.hover.textColor = brush.Color.ToUColor();
            if (font != null)
            {
                var _font = System.Windows.Forms.Application.Resources.Fonts.Find(f => f.fontNames[0] == font.Name);
                if (_font != null)
                    GUI.skin.textArea.font = _font;
                else
                    GUI.skin.textArea.font = null;
                GUI.skin.textArea.fontSize = (int)font.Size;
                bool styleBold = (font.Style & FontStyle.Bold) == FontStyle.Bold;
                bool styleItalic = (font.Style & FontStyle.Italic) == FontStyle.Italic;
                if (styleBold)
                {
                    if (styleItalic)
                        GUI.skin.textArea.fontStyle = UnityEngine.FontStyle.BoldAndItalic;
                    else
                        GUI.skin.textArea.fontStyle = UnityEngine.FontStyle.Bold;
                }
                else if (styleItalic)
                    GUI.skin.textArea.fontStyle = UnityEngine.FontStyle.Italic;
                else GUI.skin.textArea.fontStyle = UnityEngine.FontStyle.Normal;
            }
            else
            {
                var _font = System.Windows.Forms.Application.Resources.Fonts.Find(f => f.fontNames[0] == "Arial");
                if (_font != null)
                    GUI.skin.textArea.font = _font;
                GUI.skin.textArea.fontSize = 12;
            }

            if (!_group)
            {
                var c_position = Control.PointToScreen(Point.Zero);
                return GUI.TextArea(new Rect(c_position.X + x, c_position.Y + y, width, height), s);
            }
            else
            {
                var c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);
                var position = c_position - g_position + new PointF(x, y);

                return GUI.TextArea(new Rect(position.X, position.Y, width, height), s);
            }
        }
        public string DrawTextField(string s, Font font, SolidBrush brush, RectangleF layoutRectangle, HorizontalAlignment alignment)
        {
            return DrawTextField(s, font, brush, layoutRectangle.X, layoutRectangle.Y, layoutRectangle.Width, layoutRectangle.Height, alignment);
        }
        public string DrawTextField(string s, Font font, SolidBrush brush, float x, float y, float width, float height, HorizontalAlignment alignment)
        {
            if (Control == null) return s;

            GUI.skin.textField.alignment = TextAnchor.UpperLeft;
            switch (alignment)
            {
                case HorizontalAlignment.Center:
                    GUI.skin.textField.alignment = TextAnchor.MiddleCenter;
                    break;
                default:
                    GUI.skin.textField.alignment = TextAnchor.MiddleLeft;
                    break;
                case HorizontalAlignment.Right:
                    GUI.skin.textField.alignment = TextAnchor.MiddleRight;
                    break;
            }

            if (font != null)
            {
                var _font = System.Windows.Forms.Application.Resources.Fonts.Find(f => f.fontNames[0] == font.Name);
                if (_font != null)
                    GUI.skin.textField.font = _font;
                else
                    GUI.skin.textField.font = null;
                GUI.skin.textField.fontSize = (int)font.Size;
                bool styleBold = (font.Style & FontStyle.Bold) == FontStyle.Bold;
                bool styleItalic = (font.Style & FontStyle.Italic) == FontStyle.Italic;
                if (styleBold)
                {
                    if (styleItalic)
                        GUI.skin.textField.fontStyle = UnityEngine.FontStyle.BoldAndItalic;
                    else
                        GUI.skin.textField.fontStyle = UnityEngine.FontStyle.Bold;
                }
                else if (styleItalic)
                    GUI.skin.textField.fontStyle = UnityEngine.FontStyle.Italic;
                else GUI.skin.textField.fontStyle = UnityEngine.FontStyle.Normal;
            }
            else
            {
                var _font = System.Windows.Forms.Application.Resources.Fonts.Find(f => f.fontNames[0] == "Arial");
                if (_font != null)
                    GUI.skin.textField.font = _font;
                GUI.skin.textField.fontSize = 12;
            }

            GUI.color = brush.Color.ToUColor();

            if (!_group)
            {
                var c_position = Control.PointToScreen(Point.Zero);
                return GUI.TextField(new Rect(c_position.X + x, c_position.Y + y, width, height), s);
            }
            else
            {
                var c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);
                var position = c_position - g_position + new PointF(x, y);

                return GUI.TextField(new Rect(position.X, position.Y, width, height), s);
            }
        }
        public void DrawTexture(Texture texture, RectangleF layoutRectangle)
        {
            DrawTexture(texture, layoutRectangle.X, layoutRectangle.Y, layoutRectangle.Width, layoutRectangle.Height);
        }
        public void DrawTexture(Texture texture, float x, float y, float width, float height)
        {
            DrawTexture(texture, x, y, width, height, Color.White);
        }
        public void DrawTexture(Texture texture, float x, float y, float width, float height, Color color)
        {
            DrawTexture(texture, x, y, width, height, color, 0);
        }
        public void DrawTexture(Texture texture, float x, float y, float width, float height, Color color, float angle)
        {
            DrawTexture(texture, x, y, width, height, color, angle, new PointF(width / 2, height / 2));
        }
        public void DrawTexture(Texture texture, float x, float y, float width, float height, Color color, float angle, PointF pivot)
        {
            if (Control == null || texture == null) return;

            GUI.color = color.ToUColor();
            if (!_group)
            {
                var c_position = Control.PointToScreen(Point.Zero);
                if (angle != 0)
                {
                    Matrix4x4 matrixBackup = GUI.matrix;
                    GUIUtility.RotateAroundPivot(angle, new Vector2(c_position.X + x + pivot.X, c_position.Y + y + pivot.Y));
                    GUI.DrawTexture(new Rect(c_position.X + x, c_position.Y + y, width, height), texture);
                    GUI.matrix = matrixBackup;
                }
                else
                    GUI.DrawTexture(new Rect(c_position.X + x, c_position.Y + y, width, height), texture);
            }
            else
            {
                var c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);

                if (angle != 0)
                {
                    Matrix4x4 matrixBackup = GUI.matrix;
                    GUIUtility.RotateAroundPivot(angle, new Vector2(c_position.X - g_position.X + x + pivot.X, c_position.Y - g_position.Y + y + pivot.Y));
                    GUI.DrawTexture(new Rect(c_position.X - g_position.X + x, c_position.Y - g_position.Y + y, width, height), texture);
                    GUI.matrix = matrixBackup;
                }
                else
                    GUI.DrawTexture(new Rect(c_position.X - g_position.X + x, c_position.Y - g_position.Y + y, width, height), texture);
            }
        }
        public void DrawTexture(Texture texture, float x, float y, float width, float height, Material mat)
        {
            if (Control == null) return;

            GUI.color = Color.White.ToUColor();
            if (!_group)
            {
                var c_position = Control.PointToScreen(Point.Zero);
                UnityEngine.Graphics.DrawTexture(new Rect(c_position.X + x, c_position.Y + y, width, height), texture, mat);
            }
            else
            {
                var c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);

                UnityEngine.Graphics.DrawTexture(new Rect(c_position.X - g_position.X + x, c_position.Y - g_position.Y + y, width, height), texture, mat);
            }
        }
        public void FillEllipse(SolidBrush brush, float x, float y, float width, float height)
        {
            if (Control == null) return;

            GUI.color = brush.Color.ToUColor();

            if (!_group)
            {
                var c_position = Control.PointToScreen(Point.Zero);
                GUI.DrawTexture(new Rect(c_position.X + x, c_position.Y + y, width, height), System.Windows.Forms.Application.Resources.Reserved.Circle);
            }
            else
            {
                var c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);
                GUI.DrawTexture(new Rect(c_position.X - g_position.X + x, c_position.Y - g_position.Y + y, width, height), System.Windows.Forms.Application.Resources.Reserved.Circle);
            }
        }
        public void FillPolygonConvex(SolidBrush brush, PointF[] points)
        {
            if (points.Length < 3) return;

            if (DefaultMaterial != null)
                DefaultMaterial.SetPass(0);

            for (int i = 1; i + 1 < points.Length; i += 1)
            {
                GL.Begin(GL.TRIANGLES);
                

                GL.Color(brush.Color.ToUColor());
                GL.Vertex3(points[0].X, points[1].Y, 0);
                GL.Vertex3(points[i].X, points[i].Y, 0);
                GL.Vertex3(points[i + 1].X, points[i + 1].Y, 0);

                GL.End();
            }
        }
        public void FillRectangle(SolidBrush brush, Rectangle rect)
        {
            FillRectangle(brush, rect.X, rect.Y, rect.Width, rect.Height);
        }
        public void FillRectangle(SolidBrush brush, int x, int y, int width, int height)
        {
            FillRectangle(brush.Color, (float)x, (float)y, (float)width, (float)height);
        }
        public void FillRectangle(SolidBrush brush, float x, float y, float width, float height)
        {
            FillRectangle(brush.Color, x, y, width, height);
        }
        public void FillRectangle(Color color, float x, float y, float width, float height)
        {
            if (NoFill) return;
            //if (Control == null) return;
            if (color == Color.Transparent) return;

            //x += Control.Offset.X;
            //y += Control.Offset.Y;

            GUI.color = color.ToUColor();
            if (!_group)
            {
                Point c_position = Point.Empty;
                if (Control != null)
                    c_position = Control.PointToScreen(Point.Zero);

                GUI.DrawTexture(new Rect(c_position.X + x, c_position.Y + y, width, height), System.Windows.Forms.Application.DefaultSprite);
            }
            else
            {
                Point c_position = Point.Empty;
                if (Control != null)
                    c_position = Control.PointToScreen(Point.Zero);
                var g_position = _groupControlLast.PointToScreen(Point.Zero);

                GUI.DrawTexture(new Rect(c_position.X - g_position.X + x, c_position.Y - g_position.Y + y, width, height), System.Windows.Forms.Application.DefaultSprite);
                //UnityEngine.Graphics.DrawTexture(new Rect(c_position.X - g_position.X + x, c_position.Y - g_position.Y + y, width, height), System.Windows.Forms.Application.DefaultSprite, new Rect(), 0, 0, 0, 0, brush.Color.ToUColor(), DefaultMaterial);
            }
        }
        /// <summary>
        /// OnPaint call only.
        /// </summary>
        /// <param name="text"></param>
        /// <param name="font"></param>
        /// <returns></returns>
        public SizeF MeasureString(string text, Font font)
        {
            int guiSkinFontSizeBuffer = GUI_SetFont(font);

            var size = GUI.skin.label.CalcSize(new GUIContent(text));

            GUI.skin.label.fontSize = guiSkinFontSizeBuffer;

            return new SizeF(size.x, size.y);
        }
        public SizeF MeasureStringSimple(string text, Font font)
        {
            return new SizeF() { Width = text.Length * 8, Height = font.Size }; // fast but not accurate.
        }
        public SizeF MeasureString(string text, Font font, int width, StringFormat format)
        {
            return new SizeF() { Width = text.Length * 6, Height = font.Size };
        }
    }
}
