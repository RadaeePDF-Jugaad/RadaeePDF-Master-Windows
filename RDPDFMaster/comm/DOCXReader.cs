using System;
using RDDLib.docx;
using RDDLib.comm;
using RDDLib.docxv;
using RDDLib.reader;
using Windows.UI;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.Foundation;
using Windows.UI.Xaml.Shapes;
using Windows.UI.Xaml.Media.Imaging;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;

namespace com.radaee.master
{
    class DOCXReader : IContentListener
    {
        static RDLAYOUT_MODE sm_viewMode = RDLAYOUT_MODE.layout_vert;
        static public RDLAYOUT_MODE viewMode
        {
            get { return sm_viewMode; }
            set { sm_viewMode = value; }
        }
        private enum DOCXV_STATUS
        {
            STA_NONE = 0,
            STA_ZOOM = 1,
            STA_SELECT = 2,
        }
        private DOCXV_STATUS m_status;

        private double m_oldZoom;
        private int m_pageno;
        private DOCXPos m_goto_pos;
        private double m_scale;
        private bool m_keepauto;
        private DOCXDoc m_doc;
        private ScrollViewer m_scroller;
        private DOCXVContent m_content;
        private DOCXVCanvas m_canvas;//draw some temp elements, over ScrollViewer and layout view.
        private RelativePanel m_parent;
        private RDLAYOUT_MODE m_cur_mode;
        private IDOCXViewListener m_listener;
        private RDVSel m_sel;

        private bool m_autofit;
        private bool m_touched;
        private double m_hold_x;
        private double m_hold_y;
        private double m_shold_x;
        private double m_shold_y;
        public void cFillRect(ref RDRect rect, Color clr)
        {
            m_canvas.fill_rect(rect, clr);
        }
        public void cSetPos(double vx, double vy)
        {
            m_scroller.ChangeView(vx, vy, (float)m_content.vGetScale(), true);
        }
        private WriteableBitmap[] m_bmps = new WriteableBitmap[4];
        private int m_bmps_cnt = 0;
        private void FreeBmps()
        {
            for (int i = 0; i < m_bmps_cnt; i++)
            {
                //WriteableBitmap bmp = m_bmps[i];
                /*
                InMemoryRandomAccessStream mstr = new InMemoryRandomAccessStream();
                Stream str = mstr.AsStream();
                str.Write(ms_tiny_bmp, 0, 102);
                str.Flush();
                str = null;
                ulong lsz = mstr.Size;
                mstr.Seek(0);
                bmp.SetSource(mstr);
                bmp.Invalidate();
                bmp = null;
                */
                m_bmps[i] = null;
            }
            System.GC.Collect();
            m_bmps_cnt = 0;
        }
        public void cDetachBmp(WriteableBitmap bmp)
        {
            //release all memory for images
            if (m_bmps_cnt > 3) FreeBmps();
            m_bmps[m_bmps_cnt++] = bmp;
        }
        public void cAttachBmp(WriteableBitmap bmp, byte[] data)
        {
            Stream stream = bmp.PixelBuffer.AsStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            stream.Dispose();
        }
        public void cFound(bool found)
        {
            if (found) vDraw();
            if (m_listener != null) m_listener.OnDOCXFound(found);
        }
        private double to_canvasx(double x)
        {
            double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            if (tdx < m_canvas.ActualWidth)
                return x + (m_canvas.ActualWidth - tdx) * 0.5;
            else
                return x;
        }
        private double to_canvasy(double y)
        {
            double tdy = m_content.ActualHeight * m_scroller.ZoomFactor;
            double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            if (tdx >= m_parent.ActualWidth)
                return y;
            else if (tdy < m_canvas.ActualHeight)
                return y + (m_canvas.ActualHeight - tdy) * 0.5;
            else
                return y;
        }
        private double to_contx(double x)
        {
            double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            if (tdx < m_canvas.ActualWidth)
                return x - (m_canvas.ActualWidth - tdx) * 0.5;
            else
                return x;
        }
        private double to_conty(double y)
        {
            double tdy = m_content.ActualHeight * m_scroller.ZoomFactor;
            double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            if (tdx >= m_parent.ActualWidth)
                return y;
            else if (tdy < m_canvas.ActualHeight)
                return y - (m_canvas.ActualHeight - tdy) * 0.5;
            else
                return y;
        }
        private void vDraw()
        {
            if (m_content == null) return;
            m_canvas.clear();
            m_content.vDraw(m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
            m_content.vDrawFind(to_contx(m_scroller.HorizontalOffset), to_conty(m_scroller.VerticalOffset));
            if (m_listener != null && m_status != DOCXV_STATUS.STA_ZOOM)
            {
                int pageno = m_content.vGetPage(m_scroller.ActualWidth * 0.25, m_scroller.ActualHeight * 0.25);
                if (pageno != m_pageno)
                {
                    m_pageno = pageno;
                    m_listener.OnDOCXPageChanged(m_pageno);
                }
                double scale = m_content.vGetScale();
                if (scale != m_scale)
                {
                    m_scale = scale;
                    m_listener.OnDOCXScaleChanged(m_scale);
                }
            }
            if (m_sel != null) m_sel.DrawSel(m_content, m_content.vGetPage(m_sel.GetPageNo()));
        }
        public Boolean DOCXAutoFit
        {
            get { return m_autofit; }
            set
            {
                if ((m_autofit && value) || (!m_autofit && !value)) return;
                m_autofit = value;
                if (m_content != null)
                {
                    m_keepauto = true;
                    m_content.vSetAutoFit(value);
                    m_content.vResize(m_scroller.ActualWidth, m_scroller.ActualHeight, m_scroller.ZoomFactor);
                    double zoom = m_content.vGetScale();
                    if (zoom > 0)
                    {
                        m_scroller.ZoomToFactor((float)zoom);
                        //m_scroller.ChangeView(0, 0, (float)zoom);
                    }
                    if (m_goto_pos.pageno >= 0)
                    {
                        int pageno = m_goto_pos.pageno;
                        DOCXReader viewer = this;
                        m_scroller.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { viewer.DOCXGotoPage(pageno); });
                        m_goto_pos.pageno = -1;
                        m_goto_pos.x = 0;
                        m_goto_pos.y = 0;
                    }
                }
            }
        }
        public DOCXReader()
        {
            m_parent = null;
            m_pageno = -1;
            m_scale = -1;

            m_doc = null;
            m_oldZoom = -1;
            m_touched = false;
            m_autofit = true;
            m_status = DOCXV_STATUS.STA_NONE;
            m_sel = null;
            m_cur_mode = RDLAYOUT_MODE.layout_unknown;
            m_goto_pos.pageno = -1;
            m_goto_pos.x = 0;
            m_goto_pos.y = 0;
            m_keepauto = false;
        }
        ~DOCXReader()
        {
            DOCXClose();
        }
        public bool DOCXOpen(RelativePanel parent, DOCXDoc doc, RDLAYOUT_MODE lmode, IDOCXViewListener listener)
        {
            if (parent == null || doc == null) return false;

            m_parent = parent;
            m_scroller = new ScrollViewer();
            m_content = new DOCXVContent();
            m_canvas = new DOCXVCanvas();
            m_scroller.Content = m_content;

            m_doc = doc;
            m_listener = listener;
            m_pageno = -1;
            m_scale = -1;

            Windows.UI.Color clr;
            clr.A = 255;
            clr.R = 224;
            clr.G = 224;
            clr.B = 224;
            m_parent.Background = new SolidColorBrush(clr);

            m_parent.Children.Add(m_scroller);
            m_parent.Children.Add(m_canvas);
            m_scroller.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignRightWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignBottomWithPanelProperty, true);
            m_canvas.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);
            m_canvas.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
            m_canvas.SetValue(RelativePanel.AlignRightWithPanelProperty, true);
            m_canvas.SetValue(RelativePanel.AlignBottomWithPanelProperty, true);

            m_cur_mode = lmode;
            m_content.vOpen(m_doc, lmode, this);
            //all coordinate events shall from parent.
            m_parent.PointerPressed += vOnTouchDown;
            m_parent.PointerMoved += vOnTouchMove;
            m_parent.PointerReleased += vOnTouchUp;
            m_parent.PointerCanceled += vOnTouchUp;
            m_parent.PointerExited += vOnTouchUp;
            m_parent.Tapped += vOnTapped;
            m_parent.DoubleTapped += vOnDoubleTapped;

            m_scroller.SizeChanged += vOnSizeChanged;
            m_scroller.ViewChanged += vOnViewChanged;
            m_scroller.ZoomMode = ZoomMode.Enabled;
            m_scroller.IsZoomChainingEnabled = false;
            m_scroller.MinZoomFactor = 0.3f;
            m_scroller.MaxZoomFactor = 10;
            m_scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            m_scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            m_scroller.IsHoldingEnabled = true;
            m_scroller.IsScrollInertiaEnabled = true;
            m_scroller.IsHitTestVisible = true;
            if (m_scroller.ActualWidth > 0 && m_scroller.ActualHeight > 0)
            {
                bool val = m_autofit;
                m_autofit = !val;
                DOCXAutoFit = val;
                vDraw();
            }
            else m_content.vSetAutoFit(m_autofit);
            return true;
        }
        /*
        private DOCXPos m_save_pos;
        public void DOCXSaveView()
        {
            if (m_doc == null) return;
            m_save_pos = m_content.vGetPos(m_parent.ActualWidth * 0.5, m_parent.ActualHeight * 0.5);
            m_scroller.SizeChanged -= vOnSizeChanged;
            m_scroller.ViewChanged -= vOnViewChanged;
            m_parent.Children.Remove(m_canvas);

            m_scroller.Content = null;
            m_content.vClose();
            m_content.Dispose();
            m_content = null;
            FreeBmps();

            m_canvas.clear();
            m_canvas.Dispose();
            m_canvas = null;
            m_touched = false;
            m_rects_cnt = 0;
            m_sel = null;

            m_parent.PointerPressed -= vOnTouchDown;
            m_parent.PointerMoved -= vOnTouchMove;
            m_parent.PointerReleased -= vOnTouchUp;
            m_parent.PointerCanceled -= vOnTouchUp;
            m_parent.PointerExited -= vOnTouchUp;
            m_parent.Tapped -= vOnTapped;
            m_parent.DoubleTapped -= vOnDoubleTapped;
        }
        public void DOCXRestoreView()
        {
            m_content = new DOCXVContent();
            m_canvas = new DOCXVCanvas();
            m_scroller.Content = m_content;

            m_pageno = -1;
            m_scale = -1;

            Windows.UI.Color clr;
            clr.A = 255;
            clr.R = 224;
            clr.G = 224;
            clr.B = 224;
            m_parent.Background = new SolidColorBrush(clr);

            m_parent.Children.Add(m_canvas);
            m_scroller.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignRightWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignBottomWithPanelProperty, true);
            m_canvas.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);
            m_canvas.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
            m_canvas.SetValue(RelativePanel.AlignRightWithPanelProperty, true);
            m_canvas.SetValue(RelativePanel.AlignBottomWithPanelProperty, true);

            m_content.vOpen(m_doc, m_cur_mode, this);
            //all coordinate events shall from parent.
            m_parent.PointerPressed += vOnTouchDown;
            m_parent.PointerMoved += vOnTouchMove;
            m_parent.PointerReleased += vOnTouchUp;
            m_parent.PointerCanceled += vOnTouchUp;
            m_parent.PointerExited += vOnTouchUp;
            m_parent.Tapped += vOnTapped;
            m_parent.DoubleTapped += vOnDoubleTapped;

            m_scroller.SizeChanged += vOnSizeChanged;
            m_scroller.ViewChanged += vOnViewChanged;
            m_scroller.ZoomMode = ZoomMode.Enabled;
            m_scroller.IsZoomChainingEnabled = false;
            m_scroller.MinZoomFactor = 0.3f;
            m_scroller.MaxZoomFactor = 10;
            m_scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            m_scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            m_scroller.IsHoldingEnabled = true;
            m_scroller.IsScrollInertiaEnabled = true;
            m_scroller.IsHitTestVisible = true;
            //m_scroller.ChangeView(0, 0, 1);
            if (m_scroller.ActualWidth > 0 && m_scroller.ActualHeight > 0)
            {
                bool val = m_autofit;
                m_autofit = !val;
                DOCXAutoFit = val;
                if (m_save_pos.pageno >= m_doc.PageCount)
                    m_save_pos.pageno = m_doc.PageCount - 1;
                m_content.vResize(m_scroller.ActualWidth, m_scroller.ActualHeight, m_scroller.ZoomFactor);
                m_content.vSetPos(m_scroller.ActualWidth * 0.5, m_scroller.ActualHeight * 0.5, m_save_pos);
                vDraw();
            }
            else m_content.vSetAutoFit(m_autofit);
        }
        */
        public RDLAYOUT_MODE DOCXViewMode
        {
            get { return m_cur_mode; }
            set
            {
                if (m_cur_mode == value) return;
                m_cur_mode = value;
                double vw = m_scroller.ActualWidth;
                double vh = m_scroller.ActualHeight;
                double vx = vw * 0.5;
                double vy = vh * 0.5;
                DOCXPos pos = m_content.vGetPos(vw * 0.5, vh * 0.5);
                m_scroller.ZoomToFactor(1);
                m_scale = 1;
                //m_scroller.ChangeView(0, 0, 1);
                m_content.vSetView(value);
                if (vw > 0 && vh > 0) m_content.vResize(vw, vh, m_scroller.ZoomFactor);
                m_content.vSetPos(vx, vy, pos);//will update scroll view.
                //m_save_pos = pos;
            }
        }
        public void DOCXClose()
        {
            if (m_doc == null) return;
            m_scroller.SizeChanged -= vOnSizeChanged;
            m_scroller.ViewChanged -= vOnViewChanged;

            m_listener = null;
            m_scroller.Content = null;
            m_content.vClose();
            m_content.Dispose();
            m_content = null;
            FreeBmps();

            m_scroller = null;
            m_canvas.clear();
            m_canvas.Dispose();
            m_canvas = null;
            m_doc = null;
            m_touched = false;
            m_sel = null;
            m_cur_mode = RDLAYOUT_MODE.layout_unknown;

            m_parent.PointerPressed -= vOnTouchDown;
            m_parent.PointerMoved -= vOnTouchMove;
            m_parent.PointerReleased -= vOnTouchUp;
            m_parent.PointerCanceled -= vOnTouchUp;
            m_parent.PointerExited -= vOnTouchUp;
            m_parent.Tapped -= vOnTapped;
            m_parent.DoubleTapped -= vOnDoubleTapped;
            m_parent.Children.Clear();
            m_parent = null;
        }
        public int DOCXGetCurPageNo()
        {
            return m_pageno;
        }
        public DOCXPos DOCXGetCurPos()
        {
            if(m_content == null)
            {
                DOCXPos ret;
                ret.pageno = -1;
                ret.x = 0;
                ret.y = 0;
                return ret;
            }
            return m_content.vGetPos(m_scroller.ActualWidth * 0.25, m_scroller.ActualHeight * 0.25);
        }
        public float DOCXGetScale()
        {
            if (m_content == null) return -1;
            return (float)m_content.vGetScale();
        }
        public void DOCXGotoPage(int pageno)
        {
            if (m_doc == null || pageno < 0) return;
            int pcnt = m_doc.PageCount;
            if (pageno >= pcnt) pageno = pcnt - 1;
            if (m_content != null && m_content.vCanSetPos())
            {
                DOCXPos pos;
                pos.pageno = pageno;
                pos.x = 2;
                pos.y = m_doc.GetPageHeight(pageno) + 2;
                m_content.vSetPos(0, 0, pos);//will update scroll view.
            }
            else
            {
                m_goto_pos.pageno = pageno;
                m_goto_pos.x = 2;
                m_goto_pos.y = m_doc.GetPageHeight(pageno) + 2;
            }
        }
        private void vOnSizeChanged(Object sender, SizeChangedEventArgs e)
        {
            if (m_content == null) return;
            if (m_autofit)
            {
                m_autofit = false;
                DOCXAutoFit = true;
            }
            else
            {
                m_content.vResize(e.NewSize.Width, e.NewSize.Height, m_scroller.ZoomFactor);
                if (m_goto_pos.pageno >= 0)
                {
                    int pageno = m_goto_pos.pageno;
                    DOCXReader viewer = this;
                    m_scroller.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { viewer.DOCXGotoPage(pageno); });
                    m_goto_pos.pageno = -1;
                    m_goto_pos.x = 0;
                    m_goto_pos.y = 0;
                }
                vDraw();
            }
        }
        private void vOnViewChanged(Object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (m_content == null) return;
            if (m_oldZoom < 0)
            {
                m_oldZoom = m_scroller.ZoomFactor;
            }
            else if (m_oldZoom != m_scroller.ZoomFactor)
            {
                m_touched = false;
                if (m_status == DOCXV_STATUS.STA_NONE)
                {
                    m_content.vZoomStart();
                    m_status = DOCXV_STATUS.STA_ZOOM;
                }
                if (m_status == DOCXV_STATUS.STA_ZOOM)
                {
                    m_content.vZoomSet(m_scroller.ZoomFactor);
                    //m_content.vResize(m_scroller.ActualWidth, m_scroller.ActualHeight, m_scroller.ZoomFactor);
                    if (m_keepauto)
                    {
                        m_content.vSetAutoFit(true);
                        m_keepauto = false;
                        m_content.vZoomConfirm();
                        m_status = DOCXV_STATUS.STA_NONE;
                    }
                    else
                        m_autofit = false;
                }
                m_oldZoom = m_scroller.ZoomFactor;
            }
            else
            {
                if (m_status == DOCXV_STATUS.STA_ZOOM)
                {
                    m_content.vZoomConfirm();
                    m_status = DOCXV_STATUS.STA_NONE;
                }
            }
            vDraw();
        }
        private void vOnTouchDown(Object sender, PointerRoutedEventArgs e)
        {
            PointerPoint ppt = e.GetCurrentPoint(m_canvas);
            //m_parent.CapturePointer(e.Pointer);
            Point pt = ppt.Position;
            pt.X = to_contx(pt.X);
            pt.Y = to_conty(pt.Y);
            m_touched = true;
            if (OnSelTouchBegin(pt)) return;
            if (e.Pointer.PointerDeviceType == Windows.Devices.Input.PointerDeviceType.Mouse)
            {
                if (ppt.Properties.IsLeftButtonPressed)
                    OnNoneTouchBegin(pt, ppt.Timestamp);
            }
        }
        private void vOnTouchMove(Object sender, PointerRoutedEventArgs e)
        {
            if (m_touched)
            {
                PointerPoint ppt = e.GetCurrentPoint(m_canvas);
                Point pt = ppt.Position;
                pt.X = to_contx(pt.X);
                pt.Y = to_conty(pt.Y);
                if (OnSelTouchMove(pt)) return;
                OnNoneTouchMove(pt, ppt.Timestamp);
            }
        }
        private void vOnTouchUp(Object sender, PointerRoutedEventArgs e)
        {
            if (m_touched)
            {
                //m_parent.ReleasePointerCapture(e.Pointer);
                PointerPoint ppt = e.GetCurrentPoint(m_canvas);
                Point pt = ppt.Position;
                pt.X = to_contx(pt.X);
                pt.Y = to_conty(pt.Y);
                m_touched = false;
                if (OnSelTouchEnd(pt)) return;
            }
        }
        private void vOnTapped(Object sender, TappedRoutedEventArgs e)
        {
            if (m_status == DOCXV_STATUS.STA_NONE)
            {
                Point point = e.GetPosition(m_canvas);
                point.X = to_contx(point.X);
                point.Y = to_conty(point.Y);
                DOCXPos pos = m_content.vGetPos(point.X, point.Y);
                if (pos.pageno >= 0)
                {
                    long vpage = m_content.vGetPage(pos.pageno);
                    if (vpage == 0)//shall not happen
                    {
                        if (m_listener != null) m_listener.OnDOCXSingleTapped((float)point.X, (float)point.Y);
                        return;
                    }
                    DOCXPage page = m_doc.GetPage(DOCXVContent.pgGetPageNo(vpage));
                    if (page == null)
                    {
                        if (m_listener != null)
                        {
                            m_listener.OnDOCXPageTapped(pos.pageno);
                            m_listener.OnDOCXSingleTapped((float)point.X, (float)point.Y);
                        }
                        return;
                    }
                    page.ObjsStart();
                    String suri = page.GetHLink((float)pos.x, (float)pos.y);
                    if (suri != null && suri.Length > 0)//enter annotation status.
                    {
                       if (m_listener != null)
                        {
                            m_listener.OnDOCXPageTapped(DOCXVContent.pgGetPageNo(vpage));
                            m_listener.OnDOCXURI(suri);
                        }
                    }
                    else
                    {
                        if (m_listener != null)
                        {
                            m_listener.OnDOCXPageTapped(DOCXVContent.pgGetPageNo(vpage));
                            m_listener.OnDOCXSingleTapped((float)point.X, (float)point.Y);
                        }
                    }
                }
            }
        }
        private void vOnDoubleTapped(Object sender, DoubleTappedRoutedEventArgs e)
        {
            if (m_status == DOCXV_STATUS.STA_NONE)
            {
                if (m_scroller.ZoomFactor * 1.2 < m_scroller.MaxZoomFactor)
                {
                    Point point = e.GetPosition(m_canvas);
                    point.X = to_contx(point.X);
                    point.Y = to_conty(point.Y);
                    double offsetx = point.X * 0.2;
                    double offsety = point.Y * 0.2;
                    m_scroller.ChangeView(m_scroller.HorizontalOffset * 1.2 + offsetx, m_scroller.VerticalOffset * 1.2 + offsety, m_scroller.ZoomFactor * 1.2f, false);
                    //vSetPos(pos, (float)point.X, (float)point.Y);
                }
                else
                {
                    double tmp = 1 / m_scroller.ZoomFactor;
                    m_scroller.ChangeView(m_scroller.HorizontalOffset * tmp, m_scroller.VerticalOffset * tmp, 1.0f, false);
                }
                m_scroller.UpdateLayout();
            }
        }
        public void DOCXRenderPage(int pageno)
        {
            m_content.vRenderPage(m_content.vGetPage(pageno));
        }
        public void DOCXSelStart()
        {
            if (m_status == DOCXV_STATUS.STA_NONE)
            {
                m_status = DOCXV_STATUS.STA_SELECT;
                m_scroller.IsEnabled = false;
            }
        }
        public void DOCXSelEnd()
        {
            if (m_status == DOCXV_STATUS.STA_SELECT)
            {
                DOCXSelCancel();
                m_scroller.IsEnabled = true;
                m_status = DOCXV_STATUS.STA_NONE;
            }
        }
        public void DOCXSelCancel()
        {
            if (m_status == DOCXV_STATUS.STA_SELECT)
            {
                m_sel = null;
                m_status = DOCXV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public String DOCXSelGetText()
        {
            if (m_status != DOCXV_STATUS.STA_SELECT) return null;
            if (m_sel != null)
                return m_sel.GetSelString();
            return null;
        }
        public void DOCXFindStart(String pat, bool match_case, bool whole_word)
        {
            m_content.vFindStart(pat, match_case, whole_word);
        }
        public void DOCXFind(int dir)
        {
            m_content.vFind(dir);
        }
        public void DOCXFindEnd()
        {
            m_content.vFindEnd();
        }
        ulong m_tstamp;
        ulong m_tstamp_tap;
        private void OnNoneTouchBegin(Point point, ulong timestamp)
        {
            m_tstamp = timestamp;
            m_tstamp_tap = m_tstamp;
            m_hold_x = point.X;
            m_hold_y = point.Y;
            m_shold_x = m_scroller.HorizontalOffset;
            m_shold_y = m_scroller.VerticalOffset;
        }

        private void OnNoneTouchMove(Point point, ulong timestamp)
        {
            ulong del = timestamp - m_tstamp;
            if (del > 0)
            {
                double dx = point.X - m_hold_x;
                double dy = point.Y - m_hold_y;
                double vx = dx * 1000000 / del;
                double vy = dy * 1000000 / del;
                dx = 0;
                dy = 0;
                if (vx > 50 || vx < -50)
                    dx = vx;
                if (vy > 50 || vy < -50)
                    dy = vy;
                else if (timestamp - m_tstamp_tap > 1000000)//long pressed
                {
                    dx = point.X - m_hold_x;
                    dy = point.Y - m_hold_y;
                    if (dx < 10 && dx > -10 && dy < 10 && dy > -10)
                    {
                        m_status = DOCXV_STATUS.STA_NONE;
                        if (m_listener != null)
                            m_listener.OnDOCXLongPressed((float)point.X, (float)point.Y);
                    }
                }
            }
            m_scroller.ChangeView(m_shold_x + m_hold_x - point.X, m_shold_y + m_hold_y - point.Y, m_scroller.ZoomFactor, true);
        }
        private DOCXPos m_sel_pos;
        private bool OnSelTouchBegin(Point point)
        {
            if (m_status != DOCXV_STATUS.STA_SELECT) return false;
            m_hold_x = point.X;
            m_hold_y = point.Y;
            m_sel_pos = m_content.vGetPos(m_hold_x, m_hold_y);
            m_sel = new RDVSel(m_doc, m_sel_pos.pageno, to_contx(m_scroller.HorizontalOffset), to_conty(m_scroller.VerticalOffset));
            return true;
        }
        private bool OnSelTouchMove(Point point)
        {
            if (m_status != DOCXV_STATUS.STA_SELECT) return false;
            long vpage = m_content.vGetPage(m_sel_pos.pageno);
            double pdfx = DOCXVContent.pgGetPDFX(vpage, point.X + m_scroller.HorizontalOffset);
            double pdfy = DOCXVContent.pgGetPDFY(vpage, point.Y + m_scroller.VerticalOffset);
            m_sel.SetSel((float)m_sel_pos.x, (float)m_sel_pos.y, (float)pdfx, (float)pdfy);
            vDraw();
            return true;
        }
        private bool OnSelTouchEnd(Point point)
        {
            if (m_status != DOCXV_STATUS.STA_SELECT) return false;
            long vpage = m_content.vGetPage(m_sel_pos.pageno);
            double pdfx = DOCXVContent.pgGetPDFX(vpage, point.X + m_scroller.HorizontalOffset);
            double pdfy = DOCXVContent.pgGetPDFY(vpage, point.Y + m_scroller.VerticalOffset);
            m_sel.SetSel((float)m_sel_pos.x, (float)m_sel_pos.y, (float)pdfx, (float)pdfy);
            vDraw();
            if (m_listener != null)
                m_listener.OnDOCXSelected();
            return true;
        }
    }



    public class DOCXThumb : IContentListener
	{
	    public DOCXThumb()
        {
            m_parent = null;
            m_scroller = new ScrollViewer();
            m_content = new DOCXVContent();
            m_scroller.Content = m_content;
            m_page_sel = -1;

            m_doc = null;
            m_sel = null;
            m_touched = false;
        }
        ~DOCXThumb()
        {
            DOCXClose();
        }
        /*
        public void DOCXSaveView()
        {
            m_scroller.SizeChanged -= vOnSizeChanged;
            m_scroller.ViewChanged -= vOnViewChanged;

            m_parent.PointerPressed -= vOnTouchDown;
            m_parent.PointerMoved -= vOnTouchMove;
            m_parent.PointerReleased -= vOnTouchUp;
            m_parent.PointerCanceled -= vOnTouchUp;
            m_parent.PointerExited -= vOnTouchUp;
            m_parent.Tapped -= vOnTapped;

            if (m_sel != null) m_content.Children.Remove(m_sel);
            m_sel = null;
            m_content.vClose();
            FreeBmps();
            m_touched = false;
        }
        public void DOCXRestoreView()
        {
            m_page_sel = -1;

            Windows.UI.Color clr;
            clr.A = 255;
            clr.R = 224;
            clr.G = 224;
            clr.B = 224;
            m_parent.Background = new SolidColorBrush(clr);

            //clr.R = 255;
            //m_content.Background = new SolidColorBrush(clr);
            m_scroller.SizeChanged += vOnSizeChanged;
            m_scroller.ViewChanged += vOnViewChanged;
            m_scroller.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignRightWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignBottomWithPanelProperty, true);

            m_content.vOpen(m_doc, (RDLAYOUT_MODE)100, this);
            //all coordinate events shall from parent.
            m_parent.PointerPressed += vOnTouchDown;
            m_parent.PointerMoved += vOnTouchMove;
            m_parent.PointerReleased += vOnTouchUp;
            m_parent.PointerCanceled += vOnTouchUp;
            m_parent.PointerExited += vOnTouchUp;
            m_parent.Tapped += vOnTapped;

            //m_view.LayoutUpdated += OnLayout;
            m_scroller.ZoomMode = ZoomMode.Disabled;
            m_scroller.IsZoomChainingEnabled = false;
            m_scroller.MinZoomFactor = 1;
            m_scroller.MaxZoomFactor = 1;

            m_scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            m_scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            m_scroller.IsHoldingEnabled = true;
            m_scroller.IsScrollInertiaEnabled = true;
            m_scroller.IsHitTestVisible = true;
            //m_scroller.ChangeView(0, 0, 1);

            if (m_scroller.ActualWidth > 0 && m_scroller.ActualHeight > 0)
            {
                m_content.vResize(m_scroller.ActualWidth, m_scroller.ActualHeight, m_scroller.ZoomFactor);
                vDraw();
            }
        }
        */
        public bool DOCXOpen(RelativePanel parent, DOCXDoc doc, IThumbListener listener)
        {
            if (parent == null || doc == null) return false;
            m_parent = parent;
            m_doc = doc;
            m_listener = listener;
            m_page_sel = -1;

            Windows.UI.Color clr;
            clr.A = 255;
            clr.R = 224;
            clr.G = 224;
            clr.B = 224;
            m_parent.Background = new SolidColorBrush(clr);

            //clr.R = 255;
            //m_content.Background = new SolidColorBrush(clr);
            m_scroller.SizeChanged += vOnSizeChanged;
            m_scroller.ViewChanged += vOnViewChanged;
            m_parent.Children.Add(m_scroller);
            m_scroller.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignRightWithPanelProperty, true);
            m_scroller.SetValue(RelativePanel.AlignBottomWithPanelProperty, true);

            m_content.vOpen(m_doc, (RDLAYOUT_MODE)100, this);
            //all coordinate events shall from parent.
            m_parent.PointerPressed += vOnTouchDown;
            m_parent.PointerMoved += vOnTouchMove;
            m_parent.PointerReleased += vOnTouchUp;
            m_parent.PointerCanceled += vOnTouchUp;
            m_parent.PointerExited += vOnTouchUp;
            m_parent.Tapped += vOnTapped;

            //m_view.LayoutUpdated += OnLayout;
            m_scroller.ZoomMode = ZoomMode.Disabled;
            m_scroller.IsZoomChainingEnabled = false;
            m_scroller.MinZoomFactor = 1;
            m_scroller.MaxZoomFactor = 1;

            m_scroller.HorizontalScrollBarVisibility = ScrollBarVisibility.Visible;
            m_scroller.VerticalScrollBarVisibility = ScrollBarVisibility.Visible;
            m_scroller.IsHoldingEnabled = true;
            m_scroller.IsScrollInertiaEnabled = true;
            m_scroller.IsHitTestVisible = true;

            if (m_scroller.ActualWidth > 0 && m_scroller.ActualHeight > 0)
            {
                m_content.vResize(m_scroller.ActualWidth, m_scroller.ActualHeight, m_scroller.ZoomFactor);
                vDraw();
            }
            m_scroller.ChangeView(0, 0, 1);
            return true;
        }
        public void DOCXSetSelPage(int pageno)
        {
            if (m_doc == null) return;
            int pcnt = m_doc.PageCount;
            if (pageno < 0 || pageno >= pcnt) return;
            m_content.vSetPageSel(pageno);
            vDraw();
        }
        public void DOCXUpdatePage(int pageno)
        {
            long vpage = m_content.vGetPage(pageno);
            if (vpage != 0)
            {
                m_content.vRenderPage(vpage);
                vDraw();
            }
        }
        public void DOCXClose()
        {
            if (m_doc == null) return;
            m_scroller.SizeChanged -= vOnSizeChanged;
            m_scroller.ViewChanged -= vOnViewChanged;

            m_listener = null;
            if (m_parent != null)
            {
                m_parent.PointerPressed -= vOnTouchDown;
                m_parent.PointerMoved -= vOnTouchMove;
                m_parent.PointerReleased -= vOnTouchUp;
                m_parent.PointerCanceled -= vOnTouchUp;
                m_parent.PointerExited -= vOnTouchUp;
                m_parent.Tapped -= vOnTapped;


                m_parent.Children.Clear();
                m_parent = null;
            }
            if (m_sel != null) m_content.Children.Remove(m_sel);
            m_sel = null;
            m_scroller.Content = null;
            m_content.vClose();
            m_content.Dispose();
            m_content = null;
            FreeBmps();
            m_scroller = null;
            m_doc = null;
            m_touched = false;
        }
        public void cFillRect(ref RDRect rect, Windows.UI.Color clr)//to fill rectangles
		{
			if (m_sel == null)
			{
				m_sel = new Rectangle();
                m_sel.Fill = new SolidColorBrush(clr);
                m_content.Children.Add(m_sel);
            }
            m_sel.SetValue(Canvas.LeftProperty, rect.left);
            m_sel.SetValue(Canvas.TopProperty, rect.top);
            m_sel.Width = rect.right - rect.left;
			m_sel.Height = rect.bottom - rect.top;
		}
        public void cSetPos(double vx, double vy)//to set scroll position.
		{
			m_scroller.ChangeView(vx, vy, (float) m_content.vGetScale(), false);

        }
        private WriteableBitmap[] m_bmps = new WriteableBitmap[16];
        private int m_bmps_cnt = 0;
        private void FreeBmps()
        {
            for (int i = 0; i < m_bmps_cnt; i++)
            {
                //WriteableBitmap bmp = m_bmps[i];
                /*
                InMemoryRandomAccessStream mstr = new InMemoryRandomAccessStream();
                Stream str = mstr.AsStream();
                str.Write(ms_tiny_bmp, 0, 102);
                str.Flush();
                str = null;
                ulong lsz = mstr.Size;
                mstr.Seek(0);
                bmp.SetSource(mstr);
                bmp.Invalidate();
                bmp = null;
                */
                m_bmps[i] = null;
            }
            System.GC.Collect();
            m_bmps_cnt = 0;
        }
        public void cDetachBmp(WriteableBitmap bmp)
        {
            //release all memory for images
            if (m_bmps_cnt > 15) FreeBmps();
            m_bmps[m_bmps_cnt++] = bmp;
        }
        public void cAttachBmp(WriteableBitmap bmp, byte[] data)
        {
            Stream stream = bmp.PixelBuffer.AsStream();
            stream.Write(data, 0, data.Length);
            stream.Close();
            stream.Dispose();
        }
        public void cFound(bool found)//never called
		{
		}
        private void vOnSizeChanged(Object sender, SizeChangedEventArgs e)
        {
            if (m_content == null)
                return;
            m_content.vResize(e.NewSize.Width, e.NewSize.Height, m_scroller.ZoomFactor);
            vDraw();
        }
        private void vOnTouchDown(Object sender, PointerRoutedEventArgs e)
        {
            PointerPoint ppt = e.GetCurrentPoint(m_parent);
            //m_parent.CapturePointer(e.Pointer);
            Point pt = ppt.Position;
            pt.X = to_contx(pt.X);
            pt.Y = to_conty(pt.Y);
            m_touched = true;


            m_tstamp = ppt.Timestamp;
            m_hold_x = pt.X;
            m_hold_y = pt.Y;
            m_shold_x = m_scroller.HorizontalOffset;
            m_shold_y = m_scroller.VerticalOffset;
        }
        private void vOnTouchMove(Object sender, PointerRoutedEventArgs e)
        {
            if (m_touched)
            {
                PointerPoint ppt = e.GetCurrentPoint(m_parent);
                Point pt = ppt.Position;
                pt.X = to_contx(pt.X);
                pt.Y = to_conty(pt.Y);

                m_scroller.ChangeView(m_shold_x + m_hold_x - pt.X, m_shold_y + m_hold_y - pt.Y, m_scroller.ZoomFactor, true);
            }
        }
        private void vOnTouchUp(Object sender, PointerRoutedEventArgs e)
        {
            if (m_touched)
            {
                //m_parent.ReleasePointerCapture(e.Pointer);
                PointerPoint ppt = e.GetCurrentPoint(m_parent);
                Point pt = ppt.Position;
                pt.X = to_contx(pt.X);
                pt.Y = to_conty(pt.Y);
                m_touched = false;
            }
        }
        private void vOnTapped(Object sender, TappedRoutedEventArgs e)
        {
            Point point = e.GetPosition(m_parent);
            point.X = to_contx(point.X);
            point.Y = to_conty(point.Y);
            int pageno = m_content.vGetPage(point.X, point.Y);
            if (m_page_sel != pageno)
            {
                m_page_sel = pageno;
                m_content.vSetPageSel(pageno);
                if (m_listener != null) m_listener.OnPageSelected(pageno);
            }
        }
        private void vOnViewChanged(Object sender, ScrollViewerViewChangedEventArgs e)
        {
            if (m_content == null) return;
            vDraw();
        }

        //map coordinate from ScrollViewer to content View
        private double to_contx(double x)
        {
            double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            if (tdx < m_parent.ActualWidth)
                return x - (m_parent.ActualWidth - tdx) * 0.5;
            else
                return x;
        }
        private double to_conty(double y)
        {
            double tdy = m_content.ActualHeight * m_scroller.ZoomFactor;
            if (tdy < m_parent.ActualHeight)
                return y - (m_parent.ActualHeight - tdy) * 0.5;
            else
                return y;
        }
        private double to_canvasx(double x)
        {
            double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            if (tdx < m_parent.ActualWidth)
                return x + (m_parent.ActualWidth - tdx) * 0.5;
            else
                return x;
        }
        private double to_canvasy(double y)
        {
            double tdy = m_content.ActualHeight * m_scroller.ZoomFactor;
            if (tdy < m_parent.ActualHeight)
                return y + (m_parent.ActualHeight - tdy) * 0.5;
            else
                return y;
        }
        private bool m_touched;
        private double m_hold_x;
        private double m_hold_y;
        private double m_shold_x;
        private double m_shold_y;
        private ulong m_tstamp;
        /// <summary>
        /// draw the view.
        /// </summary>
        private void vDraw()
        {
            if (m_content == null)
                return;
            m_content.vDraw(m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
            if (m_sel != null)
            {
                uint oidx = (uint)m_content.Children.IndexOf(m_sel);
                uint nidx = (uint)m_content.Children.Count - 1;
                m_content.Children.Move(oidx, nidx);
            }
        }

        private int m_page_sel;
        private DOCXDoc m_doc;
		private ScrollViewer m_scroller;
		private DOCXVContent m_content;
        private RelativePanel m_parent;
        private Rectangle m_sel;
        private IThumbListener m_listener;

	}
}
