﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Windows.Forms;
using System.Drawing;
using SuperMap.Data;
using SuperMap.UI;
using SuperMap.Realspace;

namespace LineGraph.SuperMapUtility
{
    public class SampleMeasureRun
    {
        private SceneControl m_sceneControl;
        private Label m_label;

        private GeoStyle3D m_style3d;
        private Point3Ds m_point3Ds;
        private Double m_curLength = 0.0;
        private Double m_curAltitude = 0.0;
        private Double m_curArea = 0.0;
        private GeoText3D m_currentGeoText3DMessage;
        private String m_strResult;
        private Point3D m_tempPoint;

        private static String m_MeasureDistanceTagTemp = "MeasureDistanceTemp";
        private static String m_MeasureAreaTagTemp = "MeasureAreaTemp";
        private static String m_messageTag = "MeasureDistancePart";
        private static String m_messageTrackingTag = "MeasureDistanceTracking";
        private static String m_MeasureDistanceTag = "MeasureDistance";
        private static String m_MeasureAreaTag = "MeasureArea";
        private static String m_MeasureAltitudeTag = "MeasureAltitude";
        private GeoStyle3D m_GeoStyle3DTemp;
        private GeoStyle3D m_GeoStyle3D;
        /// <summary>
        /// 根据sceneControl构造 SampleRun对象
        /// </summary>
        public SampleMeasureRun(SceneControl sceneControl)
        {
            try
            {
                m_sceneControl = sceneControl;
    
                //初始化标签的状态
                m_label = new Label();
                m_label.AutoSize = true;
                m_label.BackColor = Color.White;
                m_label.Visible = false;
                m_sceneControl.Parent.Controls.Add(m_label);
                m_sceneControl.Parent.Controls.SetChildIndex(m_label, 0);

                Initialize();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private GeoText3D CreateText3DMessage()
        {
            TextPart3D textPart3D = new TextPart3D();
            textPart3D.AnchorPoint = new Point3D(0, 0, 0);
            textPart3D.Text = String.Empty;

            TextStyle style = new TextStyle();
            style.ForeColor = Color.White;
            style.IsSizeFixed = true;
            style.FontHeight = 4;
            GeoText3D text3D = new GeoText3D(textPart3D, style);
            text3D.Style3D = new GeoStyle3D();
            text3D.Style3D.AltitudeMode = AltitudeMode.RelativeToGround;
            return text3D;
        }

        /// <summary>
        /// 打开需要的地形文件和影像文件
        /// </summary>
        private void Initialize()
        {
            try
            {
                // 调整sceneControl的状态
                m_sceneControl.Action = Action3D.Pan;

                //注册事件
                m_sceneControl.Tracking += new Tracking3DEventHandler(TrackingHandler);
                m_sceneControl.Tracked += new Tracked3DEventHandler(TrackedHandler);
                m_sceneControl.MouseUp += m_SceneControl_MouseUp;

                m_point3Ds = new Point3Ds();

                m_style3d = new GeoStyle3D();

                m_GeoStyle3D = new GeoStyle3D();
                m_GeoStyle3D.MarkerColor = Color.FromArgb(255, 0, 255);
                m_GeoStyle3D.LineColor = Color.FromArgb(255, 255, 0);
                m_GeoStyle3D.LineWidth = 1;
                m_GeoStyle3D.FillForeColor = Color.FromArgb(180, Color.Violet);
                m_GeoStyle3D.AltitudeMode = AltitudeMode.ClampToGround;

                m_GeoStyle3DTemp = new GeoStyle3D();
                m_GeoStyle3DTemp.MarkerColor = Color.FromArgb(255, 0, 0);
                m_GeoStyle3DTemp.LineColor = Color.FromArgb(0, 255, 0);
                m_GeoStyle3DTemp.LineWidth = 1;
                m_GeoStyle3DTemp.FillForeColor = Color.FromArgb(180, Color.Violet);
                m_GeoStyle3DTemp.AltitudeMode = AltitudeMode.ClampToGround;

            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        // 量算结束
        private void EndOneMeasure()
        {
            try
            {
                m_curLength = 0.0;
                m_curAltitude = 0.0;
                m_curArea = 0.0;
                m_point3Ds.Clear();
                m_tempPoint = Point3D.Empty;
                m_strResult = String.Empty;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }

        // 鼠标右键，负责 显示分段量算结果
        void m_SceneControl_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                EndOneMeasure();
            }

            if (e.Button == MouseButtons.Left)
            {
                if (m_point3Ds.Count == 0)
                {
                    //ClearMeasureResult();
                }

                if (m_tempPoint != Point3D.Empty && m_point3Ds.Count > 1)
                {
                    m_point3Ds.Remove(m_point3Ds.Count - 1);
                    m_tempPoint = Point3D.Empty;
                }

                Point location = m_sceneControl.PointToClient(Cursor.Position);
                Point3D point3D = new Point3D();
                if (m_sceneControl.Action == Action3D.MeasureDistance
                    || m_sceneControl.Action == Action3D.MeasureAltitude || m_sceneControl.Action == Action3D.MeasureHorizontalDistance)
                {
                    point3D = m_sceneControl.Scene.PixelToGlobe(new Point(e.X, e.Y), SuperMap.Realspace.PixelToGlobeMode.TerrainAndModel);
                }
                else
                {
                    point3D = m_sceneControl.Scene.PixelToGlobe(new Point(e.X, e.Y));
                }

                if (!Double.IsNaN(point3D.X) && !Double.IsNaN(point3D.Y) && !Double.IsNaN(point3D.Z))
                {
                    m_point3Ds.Add(point3D);
                    if (m_sceneControl.Action == Action3D.MeasureHorizontalDistance || m_sceneControl.Action == Action3D.MeasureDistance)
                    {
                        //添加部分段长度
                        if (!Toolkit.IsZero(m_curLength))
                        {
                            if (m_point3Ds.Count >= 2)
                            {
                                m_point3Ds.RemoveRange(0, m_point3Ds.Count - 2);
                                String distanceUnit = "米";
                                GeoLine3D geoLine3D = new GeoLine3D(m_point3Ds);
                                TextPart3D textPart3D = new TextPart3D();
                                textPart3D.AnchorPoint = new Point3D(0, 0, 0);
                                Double tempCurLength = m_curLength;
                                textPart3D.Text = String.Format("{0:F2}{1}", tempCurLength, " " + distanceUnit);
                                if (m_point3Ds[0].X > 0 && m_point3Ds[1].X > 0 ||
                                    m_point3Ds[0].X < 0 && m_point3Ds[1].X < 0)
                                {
                                    textPart3D.X = geoLine3D.InnerPoint3D.X;
                                }
                                else
                                {
                                    textPart3D.X = 180;
                                }

                                textPart3D.Y = geoLine3D.InnerPoint3D.Y;
                                textPart3D.Z = geoLine3D.InnerPoint3D.Z;

                                GeoText3D text3d = m_currentGeoText3DMessage == null ? GetGeoText3DMessage() : m_currentGeoText3DMessage;
                                text3d.AddPart(textPart3D);

                                text3d.TextStyle.FontHeight = 6;
                                text3d.TextStyle.Alignment = TextAlignment.BottomLeft;
                                text3d.TextStyle.BackColor = Color.Black;
                                text3d.TextStyle.Outline = true;
                                text3d.Style3D.AltitudeMode = AltitudeMode.Absolute;

                                int index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTag);
                                if (index >= 0)
                                {
                                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                                }
                                //ClearTextMessageTag();
                                m_sceneControl.Scene.TrackingLayer.Add(text3d, m_messageTag);
                            }
                        }

                        else if (m_sceneControl.Action == Action3D.MeasureTerrainDistance)
                        {
                            //添加部分段长度
                            if (!Toolkit.IsZero(m_curLength))
                            {
                                if (m_point3Ds.Count >= 2)
                                {
                                    m_point3Ds.RemoveRange(0, m_point3Ds.Count - 2);

                                    GeoLine3D geoLine3D = new GeoLine3D(m_point3Ds);
                                    String distanceUnit = "米";
                                    TextPart3D textPart3D = new TextPart3D();
                                    textPart3D.AnchorPoint = new Point3D(0, 0, 0);
                                    Double tempCurLength = m_curLength;
                                    textPart3D.Text = String.Format("{0:F2}{1}", tempCurLength, " " + distanceUnit);
                                    if (m_point3Ds[0].X > 0 && m_point3Ds[1].X > 0 ||
                                        m_point3Ds[0].X < 0 && m_point3Ds[1].X < 0)
                                    {
                                        textPart3D.X = geoLine3D.InnerPoint.X;
                                        textPart3D.Y = geoLine3D.InnerPoint.Y;
                                    }
                                    else
                                    {
                                        textPart3D.X = 180;
                                        textPart3D.Y = geoLine3D.InnerPoint.Y;
                                    }
                                    textPart3D.Z = 0;
                                    GeoText3D text3d = GetGeoText3DMessage();
                                    text3d.AddPart(textPart3D);
                                    text3d.TextStyle.FontHeight = 6;
                                    text3d.TextStyle.Alignment = TextAlignment.BottomLeft;
                                    text3d.TextStyle.BackColor = Color.Black;
                                    text3d.TextStyle.Outline = true;
                                    text3d.Style3D.AltitudeMode = AltitudeMode.ClampToGround;
                                    ClearTextMessageTag();
                                    m_sceneControl.Scene.TrackingLayer.Add(text3d, m_messageTag);
                                }
                            }
                        }
                        else if (m_sceneControl.Action == Action3D.MeasureArea)
                        {

                        }
                        else if (m_sceneControl.Action == Action3D.MeasureAltitude)
                        {
                            //清除量算高度信息
                            if (!(Toolkit.IsZero(m_curAltitude)))
                            {
                                EndOneMeasure();
                            }
                        }

                    }
                }
            }
        }

        private void ClearMeasureResult()
        {
            try
            {
                int index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureAreaTag);
                while (index != -1)
                {
                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                    index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureAreaTag);
                }
                index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureAreaTagTemp);
                while (index != -1)
                {
                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                    index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureAreaTagTemp);
                }
                index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureDistanceTag);
                while (index != -1)
                {
                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                    index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureDistanceTag);
                }
                index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureDistanceTagTemp);
                while (index != -1)
                {
                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                    index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureDistanceTagTemp);
                }
                index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureAltitudeTag);
                while (index != -1)
                {
                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                    index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureAltitudeTag);
                }
                index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureAreaTagTemp);
                while (index != -1)
                {
                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                    index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_MeasureAreaTagTemp);
                }

                index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTag);
                while (index >= 0)
                {
                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                    index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTag);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }

        private void ClearTextMessageTag()
        {
            int index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTag);
            while (index >= 0)
            {
                m_sceneControl.Scene.TrackingLayer.Remove(index);
                index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTag);
            }
            m_currentGeoText3DMessage = null;
        }

        private GeoText3D GetGeoText3DMessage()
        {
            GeoText3D text = CreateText3DMessage();
            m_sceneControl.Scene.TrackingLayer.Add(text, m_messageTag);
            m_currentGeoText3DMessage = text;
            return text;
        }

        /// <summary>
        /// 显示最终量算的结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void TrackedHandler(object sender, Tracked3DEventArgs e)
        {
            try
            {
                if (UserHelper.bAddSymbolFill) return;
                
                // 清空临时结果
                m_point3Ds.Clear();
                m_curLength = 0.0;

                Geometry3D geometry = e.Geometry;
                Point3D textLocation = new Point3D(0, 0, 0);

                String text = String.Empty;
                if (m_sceneControl.Action == Action3D.MeasureDistance || m_sceneControl.Action == Action3D.MeasureTerrainDistance)
                {
                    //绘制量算线对象
                    GeoLine3D geoLine3D = e.Geometry.Clone() as GeoLine3D;
                    geoLine3D.Style3D = m_GeoStyle3D.Clone();
                    if (m_sceneControl.Action == Action3D.MeasureDistance)
                    {
                        geoLine3D.Style3D.AltitudeMode = AltitudeMode.Absolute;
                    }
                    else
                    {
                        geoLine3D.Style3D.AltitudeMode = AltitudeMode.ClampToGround;
                    }
                    m_sceneControl.Scene.TrackingLayer.Add(geoLine3D, m_MeasureDistanceTag);

                    //绘制量算点对象
                    Point3D point3D = Point3D.Empty;
                    for (Int32 i = 0; i < geoLine3D.PartCount; i++)
                    {
                        for (Int32 j = 0; j < geoLine3D[i].Count; j++)
                        {
                            GeoPoint3D geoPoint3D = new GeoPoint3D(geoLine3D[i][j]);
                            geoPoint3D.Style3D = m_GeoStyle3D.Clone();
                            if (m_sceneControl.Action == Action3D.MeasureDistance)
                            {
                                geoPoint3D.Style3D.AltitudeMode = AltitudeMode.Absolute;
                            }
                            else if (m_sceneControl.Action == Action3D.MeasureTerrainDistance)
                            {
                                geoPoint3D.Style3D.AltitudeMode = AltitudeMode.ClampToGround;
                            }
                            m_sceneControl.Scene.TrackingLayer.Add(geoPoint3D, m_MeasureDistanceTag);
                            point3D = geoLine3D[i][j];
                        }
                    }

                    //量算结果
                    int index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTrackingTag);
                    if (index >= 0)
                    {
                        m_sceneControl.Scene.TrackingLayer.Remove(index);
                    }

                    // 添加结果文字
                    if (m_sceneControl.Action == Action3D.MeasureDistance)
                    {
                        text = String.Format("{0}{1}{2}", "空间距离：", Math.Round(Convert.ToDecimal(e.Length), 2), "米");
                    }
                    else
                    {
                        text = String.Format("{0}{1}{2}", "依地距离：", Math.Round(Convert.ToDecimal(e.Length), 2), "米");
                    }

                    textLocation = geoLine3D[0][geoLine3D[0].Count - 1];
                    GeoText3D geoText = new GeoText3D(new TextPart3D(text, textLocation));
                    SetResultTextStyle(geoText);
                    m_sceneControl.Scene.TrackingLayer.Add(geoText, "MeasureDistance");

                    //计算首尾点高度差
                    Point3Ds point3Ds = geoLine3D[0];
                    double height = point3Ds[point3Ds.Count - 1].Z - point3Ds[0].Z;

                    //m_formMain.ClearOutputMessage();
                    //m_formMain.OutputMessage(text);
                    String message = string.Format("首尾点高度差为:{0}米。", height.ToString("##.00"));
                    //m_formMain.OutputMessage(message);
                }

                else if (m_sceneControl.Action == Action3D.MeasureArea || m_sceneControl.Action == Action3D.MeasureTerrainArea)
                {
                    //绘制量算面对象
                    GeoRegion3D geoRegion3D = e.Geometry as GeoRegion3D;
                    //绘制量算面对象
                    geoRegion3D.Style3D = m_GeoStyle3D.Clone();
                    if (m_sceneControl.Action == Action3D.MeasureArea)
                    {
                        geoRegion3D.Style3D.AltitudeMode = AltitudeMode.Absolute;
                    }
                    else
                    {
                        geoRegion3D.Style3D.AltitudeMode = AltitudeMode.ClampToGround;
                    }
                    geoRegion3D.Style3D.FillForeColor = Color.FromArgb(120, 250, 250, 50);
                    ClearTextMessageTag();
                    int index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTrackingTag);
                    if (index >= 0)
                    {
                        m_sceneControl.Scene.TrackingLayer.Remove(index);
                    }
                    m_sceneControl.Scene.TrackingLayer.Add(geoRegion3D, "geoRegion3D");

                    //绘制量算点对象
                    for (Int32 i = 0; i < geoRegion3D.PartCount; i++)
                    {
                        for (Int32 j = 0; j < geoRegion3D[i].Count; j++)
                        {
                            GeoPoint3D geoPoint3D = new GeoPoint3D(geoRegion3D[i][j]);
                            geoPoint3D.Style3D = m_GeoStyle3D.Clone();
                            if (m_sceneControl.Action == Action3D.MeasureArea)
                            {
                                geoPoint3D.Style3D.AltitudeMode = AltitudeMode.Absolute;
                            }
                            else if (m_sceneControl.Action == Action3D.MeasureTerrainArea)
                            {
                                geoPoint3D.Style3D.AltitudeMode = AltitudeMode.ClampToGround;
                            }
                            m_sceneControl.Scene.TrackingLayer.Add(geoPoint3D, m_MeasureAreaTag + i.ToString() + j.ToString());
                        }
                    }

                    ClearTextMessageTag();

                    //量算结果
                    if (m_sceneControl.Action == Action3D.MeasureArea)
                    {
                        m_strResult = String.Format("{0}{1}{2}", "空间面积：", Math.Round(Convert.ToDecimal(e.Area), 2), "平方米");
                    }
                    else
                    {
                        m_strResult = String.Format("{0}{1}{2}", "依地面积：", Math.Round(Convert.ToDecimal(e.Area), 2), "平方米");
                    }
                    GeoText3D text3d = GetGeoText3DMessage();
                    text3d[0].Text = m_strResult;
                    text3d[0].X = geoRegion3D.InnerPoint3D.X;
                    text3d[0].Y = geoRegion3D.InnerPoint3D.Y;
                    text3d[0].Z = geoRegion3D.InnerPoint3D.Z;

                    GeoText3D geoText = new GeoText3D(text3d);
                    // 添加结果文字
                    SetResultTextStyle(geoText);
                    m_sceneControl.Scene.TrackingLayer.Add(geoText, "MeasureArea");

                    //m_formMain.ClearOutputMessage();
                    //m_formMain.OutputMessage(m_strResult);
                }

                else if (m_sceneControl.Action == Action3D.MeasureAltitude)
                {
                    //绘制量算线对象
                    GeoLine3D geoLine3D = e.Geometry as GeoLine3D;
                    geoLine3D.Style3D = m_GeoStyle3D.Clone();
                    geoLine3D.Style3D.AltitudeMode = AltitudeMode.Absolute;
                    m_sceneControl.Scene.TrackingLayer.Add(geoLine3D, "Altitude");
                    // 添加结果文字
                    text = String.Format("{0}{1}{2}", "高度：", Math.Round(Convert.ToDecimal(e.Height), 2), "米");
                    textLocation = geoLine3D[0][geoLine3D[0].Count - 1];
                    GeoText3D geoText = new GeoText3D(new TextPart3D(text, textLocation));
                    SetResultTextStyle(geoText);
                    m_sceneControl.Scene.TrackingLayer.Add(geoText, "Altitude");

                    //输出首尾点高度
                    Point3Ds point3Ds = geoLine3D[0];
                    double startHeight = point3Ds[0].Z;
                    double endHeight = point3Ds[1].Z;
                    string message = string.Format("首点高度为:{0}米。", startHeight.ToString("##.00"));

                    //m_formMain.ClearOutputMessage();
                   // m_formMain.OutputMessage(message);
                    message = string.Format("尾点高度为:{0}米。", endHeight.ToString("##.00"));
                    //m_formMain.OutputMessage(message);

                    //绘制量算点对象
                    for (Int32 i = 0; i < geoLine3D.PartCount; i++)
                    {
                        for (Int32 j = 0; j < geoLine3D[i].Count; j++)
                        {
                            GeoPoint3D geoPoint3D = new GeoPoint3D(geoLine3D[i][j]);
                            geoPoint3D.Style3D = m_GeoStyle3D.Clone();
                            geoPoint3D.Style3D.AltitudeMode = AltitudeMode.Absolute;
                            m_sceneControl.Scene.TrackingLayer.Add(geoPoint3D, m_MeasureAltitudeTag + j.ToString());
                        }
                    }
                    ClearTextMessageTag();
                    if (!(Toolkit.IsZero(m_curAltitude)))
                    {
                        EndOneMeasure();
                    }
                }

                else if (m_sceneControl.Action == Action3D.MeasureHorizontalDistance)
                {
                    // 结果线
                    GeoLine3D resLine3D = e.Geometry as GeoLine3D;
                    resLine3D.Style3D = m_GeoStyle3D.Clone();
                    resLine3D.Style3D.AltitudeMode = AltitudeMode.Absolute;


                    // 结果点
                    Point3Ds resPoints = resLine3D[0];
                    for (Int32 i = 0; i < resPoints.Count; i++)
                    {
                        GeoPoint3D geoPoint = new GeoPoint3D(resPoints[i]);
                        SetGeometry3DStyle(geoPoint);
                        m_sceneControl.Scene.TrackingLayer.Add(geoPoint, "Geometry" + i.ToString());
                    }

                    EndOneMeasure();
                    m_sceneControl.Scene.TrackingLayer.Add(resLine3D, "Geometry");

                    // 添加结果文字
                    text = String.Format("{0}{1}{2}", "总长度： ", Math.Round(Convert.ToDecimal(e.Length), 2), "米");
                    GeoLine3D line = (geometry as GeoLine3D);
                    textLocation = line[0][line[0].Count - 2];

                    //ClearTextMessageTag();
                    int index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTrackingTag);
                    if (index >= 0)
                    {
                        m_sceneControl.Scene.TrackingLayer.Remove(index);
                    }
                    GeoText3D geoText = new GeoText3D(new TextPart3D(text, textLocation));
                    SetResultTextStyle(geoText);

                    m_sceneControl.Scene.TrackingLayer.Add(geoText, "MeasureResult");

                    //m_formMain.ClearOutputMessage();
                    //m_formMain.OutputMessage(text);

                    //计算首尾点高度差
                    Point3Ds point3Ds = resLine3D[0];
                    double height = point3Ds[point3Ds.Count - 1].Z - point3Ds[0].Z;
                    string message = string.Format("首尾点高度差为:{0}米。", height.ToString("##.00"));
                    //m_formMain.OutputMessage(message);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                m_label.Visible = false;
            }
        }

        /// <summary>
        /// 设置添加到TrackingLayer的三维几何对象的风格
        /// </summary>
        /// <param name="geometry"></param>
        private void SetGeometry3DStyle(Geometry3D geometry)
        {
            try
            {
                GeoStyle3D style = new GeoStyle3D();

                if (m_sceneControl.Action == Action3D.MeasureAltitude || m_sceneControl.Action == Action3D.MeasureDistance || m_sceneControl.Action == Action3D.MeasureHorizontalDistance)
                {
                    style.AltitudeMode = AltitudeMode.Absolute;
                }
                else
                {
                    style.AltitudeMode = AltitudeMode.ClampToGround;
                }

                style.MarkerSize = 4;
                style.MarkerColor = Color.FromArgb(255, 0, 255);

                style.LineColor = Color.Yellow;
                style.LineWidth = 2;
                style.FillMode = FillMode3D.LineAndFill;
                style.FillForeColor = Color.LightSeaGreen;
                geometry.Style3D = style;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 设置量算结果文本的风格
        /// </summary>
        /// <param name="text"></param>
        private void SetResultTextStyle(GeoText3D text)
        {
            try
            {
                TextStyle textStyle = new TextStyle();
                textStyle.ForeColor = Color.White;
                textStyle.Outline = true;
                textStyle.BackColor = Color.Black;
                textStyle.FontHeight = 10;
                text.TextStyle = textStyle;
                GeoStyle3D style = new GeoStyle3D();
                if (m_sceneControl.Action == Action3D.MeasureAltitude)
                {
                    style.AltitudeMode = AltitudeMode.Absolute;
                }
                else
                {
                    style.AltitudeMode = AltitudeMode.ClampToGround;
                }
                text.Style3D = style;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 标签中显示当前跟踪量算的结果
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void TrackingHandler(object sender, Tracking3DEventArgs e)
        {
            try
            {
                OutputMeasureResult(e);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 开始距离量算
        /// </summary>
        public void BeginMeasureDistance()
        {
            m_sceneControl.Action = Action3D.MeasureDistance;
        }

        /// <summary>
        /// 开始依地距离量算
        /// </summary>
        public void BeginMeasureTerrainDistance()
        {
            m_sceneControl.Action = Action3D.MeasureTerrainDistance;
        }

        /// <summary>
        /// 开始水平距离量算
        /// </summary>
        public void BeginMeasureHorizontalDistance()
        {
            m_sceneControl.Action = Action3D.MeasureHorizontalDistance;
        }

        /// <summary>
        /// 开始面积量算
        /// </summary>
        public void BeginMeasureArea()
        {
            m_sceneControl.Action = Action3D.MeasureArea;
        }

        /// <summary>
        /// 开始高度量算
        /// </summary>
        public void BeginMeasureAltitude()
        {
            m_sceneControl.Action = Action3D.MeasureAltitude;
        }

        /// <summary>
        /// 清空量算结果
        /// </summary>
        public void ClearResult()
        {
            try
            {
                m_sceneControl.Scene.TrackingLayer.Clear();
                m_sceneControl.Action = Action3D.Pan;
                m_currentGeoText3DMessage = null;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// 开始依地面积量算
        /// </summary>
        public void BeginMeasureTerrainArea()
        {
            m_sceneControl.Action = Action3D.MeasureTerrainArea;
        }


        // 输出量算结果
        private void OutputMeasureResult(SuperMap.UI.Tracking3DEventArgs e)
        {
            if (m_sceneControl.Action == Action3D.MeasureDistance || m_sceneControl.Action == Action3D.MeasureTerrainDistance || m_sceneControl.Action == Action3D.MeasureHorizontalDistance)
            {
                if (e.CurrentLength > 0)
                {
                    m_curLength = e.CurrentLength;
                    Double tempCurLength = m_curLength;
                    Double tempTotalLength = e.TotalLength;
                    m_strResult = String.Format("    ", tempCurLength, " " + "米", tempTotalLength, " " + "米");
                    OutputMeasureDistance(e);
                }
            }

            if (m_sceneControl.Action == Action3D.MeasureArea || m_sceneControl.Action == Action3D.MeasureTerrainArea)
            {
                Double area = e.TotalArea;
                m_strResult = String.Format("", area, " " + "米");

                if (!Toolkit.IsZero(e.TotalArea))
                {
                    OutputMeasureArea(e);
                }
            }

            if (m_sceneControl.Action == Action3D.MeasureAltitude)
            {
                Double currentHeight = e.CurrentHeight;
                m_strResult = String.Format("    ", currentHeight, " " + "米");
                OutputMeasureAltitude(e);
            }
        }

        private void OutputMeasureArea(SuperMap.UI.Tracking3DEventArgs e)
        {
            try
            {
                Point location = m_sceneControl.PointToClient(Cursor.Position);

                if (m_tempPoint != Point3D.Empty)
                {
                    m_point3Ds.Remove(m_point3Ds.Count - 1);
                }
                m_tempPoint = new Point3D(e.X, e.Y, e.Z);
                m_point3Ds.Add(m_tempPoint);

                GeoRegion3D geoRegion3D = null;
                if (m_point3Ds.Count >= 3)
                {
                    geoRegion3D = new GeoRegion3D(m_point3Ds);
                    geoRegion3D.Style3D = m_GeoStyle3DTemp.Clone();

                    location.Offset(30, 30);
                    if (location.X > m_sceneControl.Bounds.Width / 4 * 3)
                    {
                        location.X = m_sceneControl.Bounds.Width / 4 * 3;
                    }
                    if (location.Y > m_sceneControl.Bounds.Height)
                    {
                        location.Y = location.Y - 60;
                    }

                    TextPart3D textPart3D = new TextPart3D();
                    textPart3D.AnchorPoint = new Point3D(0, 0, 0);
                    textPart3D.Text = String.Empty;

                    TextStyle style = new TextStyle();
                    style.ForeColor = Color.White;
                    style.IsSizeFixed = true;
                    style.FontHeight = 6;
                    style.Alignment = TextAlignment.BottomLeft;
                    style.BackColor = Color.Black;
                    style.Outline = true;
                    GeoText3D text3d = new GeoText3D(textPart3D, style);
                    text3d.Style3D = new GeoStyle3D();
                    text3d.Style3D.AltitudeMode = AltitudeMode.Absolute;

                    text3d[0].Text = e.TotalArea.ToString("##.00") + "平方米";
                    if (e.Geometry != null)
                    {
                        text3d[0].X = e.Geometry.InnerPoint.X;
                        text3d[0].Y = e.Geometry.InnerPoint.Y;
                    }
                    else
                    {
                        text3d[0].X = geoRegion3D.InnerPoint.X;
                        text3d[0].Y = geoRegion3D.InnerPoint.Y;
                    }

                    int index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTrackingTag);
                    if (index >= 0)
                    {
                        m_sceneControl.Scene.TrackingLayer.Remove(index);
                    }
                    m_sceneControl.Scene.TrackingLayer.Add(text3d, m_messageTrackingTag);
                    m_curArea = e.TotalArea;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
        private void OutputMeasureDistance(SuperMap.UI.Tracking3DEventArgs e)
        {
            try
            {
                Point location = m_sceneControl.PointToClient(Cursor.Position);

                if (m_tempPoint != Point3D.Empty && m_point3Ds.Count > 1)
                {
                    m_point3Ds.Remove(m_point3Ds.Count - 1);
                }
                m_tempPoint = new Point3D(e.X, e.Y, e.Z);
                m_point3Ds.Add(m_tempPoint);

                location.Offset(30, 30);
                if (location.X > m_sceneControl.Bounds.Width / 3 * 2)
                {
                    location.X = m_sceneControl.Bounds.Width / 3 * 2;
                }
                if (location.Y > m_sceneControl.Bounds.Height)
                {
                    location.Y = location.Y - 60;
                }

                TextPart3D textPart3D = new TextPart3D();
                textPart3D.AnchorPoint = new Point3D(0, 0, 0);
                textPart3D.Text = String.Empty;

                TextStyle style = new TextStyle();
                style.ForeColor = Color.White;
                style.IsSizeFixed = true;
                style.FontHeight = 6;
                style.Alignment = TextAlignment.BottomLeft;
                style.BackColor = Color.Black;
                style.Outline = true;
                GeoText3D text3d = new GeoText3D(textPart3D, style);
                text3d.Style3D = new GeoStyle3D();
                text3d.Style3D.AltitudeMode = AltitudeMode.Absolute;

                text3d[0].Text = e.CurrentLength.ToString("##.00") + "米";
                Point3D lastPoint = Point3D.Empty;
                if (e.Geometry != null)
                {
                    Point3Ds points = (e.Geometry as GeoLine3D)[0];
                    lastPoint = points[points.Count - 1];
                }
                else
                {
                    lastPoint = m_point3Ds[0];
                }
                text3d[0].X = (lastPoint.X + e.X) / 2;
                text3d[0].Y = (lastPoint.Y + e.Y) / 2;

                int index = m_sceneControl.Scene.TrackingLayer.IndexOf(m_messageTrackingTag);
                if (index >= 0)
                {
                    m_sceneControl.Scene.TrackingLayer.Remove(index);
                }
                m_sceneControl.Scene.TrackingLayer.Add(text3d, m_messageTrackingTag);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }

        private void OutputMeasureAltitude(SuperMap.UI.Tracking3DEventArgs e)
        {
            try
            {
                Point location = m_sceneControl.PointToClient(Cursor.Position);
                location.Offset(30, 30);
                if (location.X > m_sceneControl.Bounds.Width / 5 * 4)
                {
                    location.X = m_sceneControl.Bounds.Width / 5 * 4;
                }
                if (location.Y > m_sceneControl.Bounds.Height)
                {
                    location.Y = location.Y - 60;
                }

                GeoText3D text3d = GetGeoText3DMessage();

                text3d.TextStyle.FontHeight = 6;
                text3d.TextStyle.Alignment = TextAlignment.BottomLeft;
                text3d.TextStyle.BackColor = Color.Black;
                text3d.TextStyle.Outline = true;
                text3d.Style3D.AltitudeMode = AltitudeMode.RelativeToGround;

                text3d[0].Text = e.CurrentHeight.ToString("##.00") + "米";
                text3d[0].X = e.X;
                text3d[0].Y = e.Y;
                text3d[0].Z = e.Z + e.CurrentHeight;

                //MessageBox.Show(text3d[0].Z);

                text3d.Style3D.AltitudeMode = AltitudeMode.Absolute;

                ClearTextMessageTag();
                m_sceneControl.Scene.TrackingLayer.Add(text3d, m_messageTag);
                m_curAltitude = e.CurrentHeight;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
            }
        }
    }
}
