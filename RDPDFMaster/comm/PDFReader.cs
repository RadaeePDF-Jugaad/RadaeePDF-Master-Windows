using RDDLib.comm;
using RDDLib.pdf;
using RDDLib.pdfv;
using RDDLib.reader;
using System;
using System.IO;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.ApplicationModel;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Shapes;

namespace com.radaee.master
{
    class PDFReader : IContentListener
    {
        static int m_blk_gap = 8;
        static bool vInRect(RDRect rect, Point pt)
        {
            return (pt.X >= rect.left && pt.Y >= rect.top && pt.X <= rect.right && pt.Y <= rect.bottom);
        }
        static bool vOnBlk(float x, float y, Point pt)
        {
            RDRect rcb;
            rcb.left = x - m_blk_gap;
            rcb.top = y - m_blk_gap;
            rcb.right = x + m_blk_gap;
            rcb.bottom = y + m_blk_gap;
            return (pt.X >= rcb.left && pt.Y >= rcb.top && pt.X <= rcb.right && pt.Y <= rcb.bottom);
        }
        static bool vOnBlk(float x, float y, RDPoint pt)
        {
            RDRect rcb;
            rcb.left = x - m_blk_gap;
            rcb.top = y - m_blk_gap;
            rcb.right = x + m_blk_gap;
            rcb.bottom = y + m_blk_gap;
            return (pt.x >= rcb.left && pt.y >= rcb.top && pt.x <= rcb.right && pt.y <= rcb.bottom);
        }
        private void vOnDrawBlk(float x, float y)
        {
            RDRect rcb;
            rcb.left = x - m_blk_gap;
            rcb.top = y - m_blk_gap;
            rcb.right = x + m_blk_gap;
            rcb.bottom = y + m_blk_gap;
            m_canvas.fill_rect(rcb, Color.FromArgb(255, 255, 255, 255));
            m_canvas.draw_rect(rcb, 1, 0xFF000000);
        }
        private interface IAnnoitOP
        {
            void OnDraw();
            void OnTouchBeg(Point point);
            void OnTouchMove(Point point);
            void OnTouchEnd(Point point);
        }
        private class AnnotOpNormal : IAnnoitOP
        {
            private PDFReader m_reader;
            public AnnotOpNormal(PDFReader reader)
            {
                m_reader = reader;
            }
            public void OnDraw()
            {
                PDFReader thiz = m_reader;
                float dx = (float)(thiz.m_shold_x - thiz.m_hold_x);
                float dy = (float)(thiz.m_shold_y - thiz.m_hold_y);
                RDRect rect = thiz.m_annot_rect;
                //RDRect rect = m_annot.Rect;
                rect.left += dx;
                rect.top += dy;
                rect.right += dx;
                rect.bottom += dy;
                rect.left = (float)thiz.to_canvasx(rect.left);
                rect.top = (float)thiz.to_canvasy(rect.top);
                rect.right = (float)thiz.to_canvasx(rect.right);
                rect.bottom = (float)thiz.to_canvasy(rect.bottom);
                thiz.m_canvas.draw_rect(rect, 1, 0xFF000000);
            }
            public void OnTouchBeg(Point point)
            {
            }
            public void OnTouchEnd(Point point)
            {
                PDFReader thiz = m_reader;
                thiz.m_modified = true;
                float dx = (float)(point.X - thiz.m_hold_x);
                float dy = (float)(point.Y - thiz.m_hold_y);
                thiz.m_annot_rect.left += dx;
                thiz.m_annot_rect.top += dy;
                thiz.m_annot_rect.right += dx;
                thiz.m_annot_rect.bottom += dy;
                PDFPos pos = thiz.m_content.vGetPos(point.X, point.Y);
                long vpage = thiz.m_content.vGetPage(thiz.m_annot_pos.pageno);
                if (pos.pageno == thiz.m_annot_pos.pageno)
                {
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                    thiz.m_annot_rect = mat.TransformRect(thiz.m_annot_rect);
                    RDRect rect = thiz.m_annot.Rect;
                    thiz.m_annot.Rect = thiz.m_annot_rect;
                    thiz.m_opstack.push(PDFOPStack.new_move(pos.pageno, rect, pos.pageno, thiz.m_annot.IndexInPage, thiz.m_annot_rect));
                    thiz.m_content.vRenderPage(vpage);
                    if (thiz.m_listener != null) thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                }
                else
                {
                    long vdest = thiz.m_content.vGetPage(pos.pageno);
                    PDFPage dpage = thiz.m_doc.GetPage(pos.pageno);
                    if (dpage != null)
                    {
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vdest, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                        RDRect rect = thiz.m_annot.Rect;
                        thiz.m_annot_rect = mat.TransformRect(thiz.m_annot_rect);
                        thiz.m_opstack.push(PDFOPStack.new_move(thiz.m_annot_pos.pageno, rect, pos.pageno, dpage.AnnotCount, thiz.m_annot_rect));
                        dpage.ObjsStart();
                        thiz.m_annot.MoveToPage(dpage, thiz.m_annot_rect);
                        dpage.Close();
                        thiz.m_content.vRenderPage(vpage);
                        thiz.m_content.vRenderPage(vdest);
                        if (thiz.m_listener != null)
                        {
                            thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                            thiz.m_listener.OnPDFPageUpdated(thiz.m_annot_pos.pageno);
                        }
                    }
                }
                thiz.PDFAnnotEnd();
            }
            public void OnTouchMove(Point point)
            {
            }
        }
        private class AnnotOpResize : IAnnoitOP
        {
            private PDFReader m_reader;
            private bool m_attached;
            private RDRect m_rect;
            private int m_node;
            public AnnotOpResize(PDFReader reader)
            {
                m_reader = reader;
                m_node = 0;
                m_attached = false;
            }
            private void attach()
            {
                PDFReader thiz = m_reader;
                if (!m_attached)
                {
                    float dx = (float)(thiz.m_shold_x - thiz.m_hold_x);
                    float dy = (float)(thiz.m_shold_y - thiz.m_hold_y);
                    m_rect = thiz.m_annot_rect;
                    m_rect.left += dx;
                    m_rect.top += dy;
                    m_rect.right += dx;
                    m_rect.bottom += dy;
                    m_rect.left = (float)thiz.to_canvasx(m_rect.left);
                    m_rect.top = (float)thiz.to_canvasy(m_rect.top);
                    m_rect.right = (float)thiz.to_canvasx(m_rect.right);
                    m_rect.bottom = (float)thiz.to_canvasy(m_rect.bottom);
                    m_attached = true;
                }
            }
            public void OnDraw()
            {
                attach();
                PDFReader thiz = m_reader;
                float midx = (m_rect.left + m_rect.right) * 0.5f;
                float midy = (m_rect.top + m_rect.bottom) * 0.5f;
                thiz.m_canvas.draw_rect(m_rect, 1, 0xFF000000);
                thiz.vOnDrawBlk(m_rect.left, m_rect.top);
                thiz.vOnDrawBlk(midx, m_rect.top);
                thiz.vOnDrawBlk(m_rect.right, m_rect.top);
                thiz.vOnDrawBlk(m_rect.right, midy);
                thiz.vOnDrawBlk(m_rect.right, m_rect.bottom);
                thiz.vOnDrawBlk(midx, m_rect.bottom);
                thiz.vOnDrawBlk(m_rect.left, m_rect.bottom);
                thiz.vOnDrawBlk(m_rect.left, midy);
            }
            public void OnTouchBeg(Point point)
            {
                attach();
                PDFReader thiz = m_reader;

                RDRect rect = thiz.m_annot_rect;
                rect.left = (float)thiz.to_canvasx(rect.left);
                rect.top = (float)thiz.to_canvasy(rect.top);
                rect.right = (float)thiz.to_canvasx(rect.right);
                rect.bottom = (float)thiz.to_canvasy(rect.bottom);
                float midx = (rect.left + rect.right) * 0.5f;
                float midy = (rect.top + rect.bottom) * 0.5f;

                if (vOnBlk(rect.left, rect.top, point)) { m_node = 1; return; }
                if (vOnBlk(midx, rect.top, point)) { m_node = 2; return; }
                if (vOnBlk(rect.right, rect.top, point)) { m_node = 3; return; }
                if (vOnBlk(rect.right, midy, point)) { m_node = 4; return; }
                if (vOnBlk(rect.right, rect.bottom, point)) { m_node = 5; return; }
                if (vOnBlk(midx, rect.bottom, point)) { m_node = 6; return; }
                if (vOnBlk(rect.left, rect.bottom, point)) { m_node = 7; return; }
                if (vOnBlk(rect.left, midy, point)) { m_node = 8; return; }

                if (vInRect(rect, point)) m_node = 9;
                else m_node = 0;
            }
            private RDRect moveRect(float dx, float dy)
            {
                RDRect rect = m_reader.m_annot_rect;
                if (m_node > 0)//select node
                {
                    switch (m_node)
                    {
                        case 1:
                            rect.left += dx;
                            rect.top += dy;
                            break;
                        case 2:
                            rect.top += dy;
                            break;
                        case 3:
                            rect.right += dx;
                            rect.top += dy;
                            break;
                        case 4:
                            rect.right += dx;
                            break;
                        case 5:
                            rect.right += dx;
                            rect.bottom += dy;
                            break;
                        case 6:
                            rect.bottom += dy;
                            break;
                        case 7:
                            rect.left += dx;
                            rect.bottom += dy;
                            break;
                        case 8:
                            rect.left += dx;
                            break;
                        default:
                            rect.left += dx;
                            rect.top += dy;
                            rect.right += dx;
                            rect.bottom += dy;
                            break;
                    }
                }
                return rect;
            }
            public void OnTouchEnd(Point point)
            {
                PDFReader thiz = m_reader;
                m_attached = false;
                if (m_node == 0)//invalidate
                {
                    thiz.PDFAnnotEnd();
                    return;
                }
                thiz.m_modified = true;
                float dx = (float)(point.X - thiz.m_hold_x);
                float dy = (float)(point.Y - thiz.m_hold_y);
                PDFPos pos = thiz.m_content.vGetPos(point.X, point.Y);
                long vpage = thiz.m_content.vGetPage(thiz.m_annot_pos.pageno);
                if (m_node != 9)//select node
                {
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                    RDRect rect = moveRect(dx, dy);
                    thiz.m_annot_rect = mat.TransformRect(rect);
                    thiz.m_annot.Rect = thiz.m_annot_rect;
                    thiz.m_content.vRenderPage(vpage);
                    if (thiz.m_listener != null) thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                }
                else
                {
                    thiz.m_annot_rect.left += dx;
                    thiz.m_annot_rect.top += dy;
                    thiz.m_annot_rect.right += dx;
                    thiz.m_annot_rect.bottom += dy;
                    if (pos.pageno == thiz.m_annot_pos.pageno)
                    {
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                        thiz.m_annot_rect = mat.TransformRect(thiz.m_annot_rect);
                        RDRect rect = thiz.m_annot.Rect;
                        thiz.m_annot.Rect = thiz.m_annot_rect;
                        thiz.m_opstack.push(PDFOPStack.new_move(pos.pageno, rect, pos.pageno, thiz.m_annot.IndexInPage, thiz.m_annot_rect));
                        thiz.m_content.vRenderPage(vpage);
                        if (thiz.m_listener != null) thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                    }
                    else
                    {
                        long vdest = thiz.m_content.vGetPage(pos.pageno);
                        PDFPage dpage = thiz.m_doc.GetPage(pos.pageno);
                        if (dpage != null)
                        {
                            RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vdest, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                            RDRect rect = thiz.m_annot.Rect;
                            thiz.m_annot_rect = mat.TransformRect(thiz.m_annot_rect);
                            thiz.m_opstack.push(PDFOPStack.new_move(thiz.m_annot_pos.pageno, rect, pos.pageno, dpage.AnnotCount, thiz.m_annot_rect));
                            dpage.ObjsStart();
                            thiz.m_annot.MoveToPage(dpage, thiz.m_annot_rect);
                            dpage.Close();
                            thiz.m_content.vRenderPage(vpage);
                            thiz.m_content.vRenderPage(vdest);
                            if (thiz.m_listener != null)
                            {
                                thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                                thiz.m_listener.OnPDFPageUpdated(thiz.m_annot_pos.pageno);
                            }
                        }
                    }
                }
                thiz.PDFAnnotEnd();
            }
            public void OnTouchMove(Point point)
            {
                PDFReader thiz = m_reader;

                float dx = (float)(thiz.m_shold_x - thiz.m_hold_x);
                float dy = (float)(thiz.m_shold_y - thiz.m_hold_y);
                RDRect rect = moveRect(dx, dy);
                m_rect.left = (float)thiz.to_canvasx(rect.left);
                m_rect.top = (float)thiz.to_canvasy(rect.top);
                m_rect.right = (float)thiz.to_canvasx(rect.right);
                m_rect.bottom = (float)thiz.to_canvasy(rect.bottom);
            }
        }
        private class AnnotOpLine : IAnnoitOP
        {
            private PDFReader m_reader;
            private bool m_attached;
            private RDRect m_rect;
            private RDPoint m_pt10;
            private RDPoint m_pt20;
            private RDPoint m_pt1;
            private RDPoint m_pt2;
            private int m_node;
            public AnnotOpLine(PDFReader reader)
            {
                m_reader = reader;
                m_node = 0;
                m_attached = false;
            }
            private void attach()
            {
                PDFReader thiz = m_reader;
                if (!m_attached)
                {
                    float dx = (float)(thiz.m_shold_x - thiz.m_hold_x);
                    float dy = (float)(thiz.m_shold_y - thiz.m_hold_y);
                    m_rect = thiz.m_annot_rect;
                    m_rect.left += dx;
                    m_rect.top += dy;
                    m_rect.right += dx;
                    m_rect.bottom += dy;
                    m_rect.left = (float)thiz.to_canvasx(m_rect.left);
                    m_rect.top = (float)thiz.to_canvasy(m_rect.top);
                    m_rect.right = (float)thiz.to_canvasx(m_rect.right);
                    m_rect.bottom = (float)thiz.to_canvasy(m_rect.bottom);
                    m_pt1 = thiz.m_annot.GetLinePoint(0);
                    m_pt2 = thiz.m_annot.GetLinePoint(1);

                    long vpage = thiz.m_content.vGetPage(thiz.m_annot_pos.pageno);
                    m_pt1.x = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, m_pt1.x));
                    m_pt2.x = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, m_pt2.x));
                    m_pt1.y = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, m_pt1.y));
                    m_pt2.y = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, m_pt2.y));

                    float tmp = (float)thiz.m_scroller.HorizontalOffset;
                    m_pt1.x -= tmp;
                    m_pt2.x -= tmp;
                    tmp = (float)thiz.m_scroller.VerticalOffset;
                    m_pt1.y -= tmp;
                    m_pt2.y -= tmp;

                    m_pt10 = m_pt1;
                    m_pt20 = m_pt2;
                    m_attached = true;
                }
            }
            public void OnDraw()
            {
                attach();
                PDFReader thiz = m_reader;
                thiz.m_canvas.draw_rect(m_rect, 1, 0xFF000000);
                thiz.m_canvas.draw_lines_and_blks(new RDPoint[] { m_pt1, m_pt2 }, 2, 2, m_blk_gap, 0x400000FF);
            }
            public void OnTouchBeg(Point point)
            {
                attach();
                PDFReader thiz = m_reader;

                RDPoint pt1 = m_pt1;
                RDPoint pt2 = m_pt2;
                pt1.x = (float)thiz.to_canvasx(pt1.x);
                pt1.y = (float)thiz.to_canvasy(pt1.y);
                pt2.x = (float)thiz.to_canvasx(pt2.x);
                pt2.y = (float)thiz.to_canvasy(pt2.y);
                if (vOnBlk(pt1.x, pt1.y, point)) { m_node = 1; return; }
                if (vOnBlk(pt2.x, pt2.y, point)) { m_node = 2; return; }

                RDRect rect = thiz.m_annot_rect;
                rect.left = (float)thiz.to_canvasx(rect.left);
                rect.top = (float)thiz.to_canvasy(rect.top);
                rect.right = (float)thiz.to_canvasx(rect.right);
                rect.bottom = (float)thiz.to_canvasy(rect.bottom);
                if (vInRect(rect, point)) m_node = 3;
                else m_node = 0;
            }
            public void OnTouchEnd(Point point)
            {
                PDFReader thiz = m_reader;
                m_attached = false;
                if (m_node == 0)//invalidate
                {
                    thiz.PDFAnnotEnd();
                    return;
                }
                thiz.m_modified = true;
                float dx = (float)(point.X - thiz.m_hold_x);
                float dy = (float)(point.Y - thiz.m_hold_y);
                PDFPos pos = thiz.m_content.vGetPos(point.X, point.Y);
                long vpage = thiz.m_content.vGetPage(thiz.m_annot_pos.pageno);
                if (m_node != 3)//select node
                {
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                    RDPoint pt1 = m_pt10;
                    RDPoint pt2 = m_pt20;
                    if (m_node > 0)//select node
                    {
                        switch (m_node)
                        {
                            case 1:
                                pt1.x += dx;
                                pt1.y += dy;
                                break;
                            case 2:
                                pt2.x += dx;
                                pt2.y += dy;
                                break;
                        }
                    }
                    pt1 = mat.TransformPoint(pt1);
                    pt2 = mat.TransformPoint(pt2);
                    thiz.m_annot.SetLinePoint(pt1.x, pt1.y, pt2.x, pt2.y);
                    thiz.m_content.vRenderPage(vpage);
                    if (thiz.m_listener != null) thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                }
                else
                {
                    thiz.m_annot_rect.left += dx;
                    thiz.m_annot_rect.top += dy;
                    thiz.m_annot_rect.right += dx;
                    thiz.m_annot_rect.bottom += dy;
                    if (pos.pageno == thiz.m_annot_pos.pageno)
                    {
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                        thiz.m_annot_rect = mat.TransformRect(thiz.m_annot_rect);
                        RDRect rect = thiz.m_annot.Rect;
                        thiz.m_annot.Rect = thiz.m_annot_rect;
                        thiz.m_opstack.push(PDFOPStack.new_move(pos.pageno, rect, pos.pageno, thiz.m_annot.IndexInPage, thiz.m_annot_rect));
                        thiz.m_content.vRenderPage(vpage);
                        if (thiz.m_listener != null) thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                    }
                    else
                    {
                        long vdest = thiz.m_content.vGetPage(pos.pageno);
                        PDFPage dpage = thiz.m_doc.GetPage(pos.pageno);
                        if (dpage != null)
                        {
                            RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vdest, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                            RDRect rect = thiz.m_annot.Rect;
                            thiz.m_annot_rect = mat.TransformRect(thiz.m_annot_rect);
                            thiz.m_opstack.push(PDFOPStack.new_move(thiz.m_annot_pos.pageno, rect, pos.pageno, dpage.AnnotCount, thiz.m_annot_rect));
                            dpage.ObjsStart();
                            thiz.m_annot.MoveToPage(dpage, thiz.m_annot_rect);
                            dpage.Close();
                            thiz.m_content.vRenderPage(vpage);
                            thiz.m_content.vRenderPage(vdest);
                            if (thiz.m_listener != null)
                            {
                                thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                                thiz.m_listener.OnPDFPageUpdated(thiz.m_annot_pos.pageno);
                            }
                        }
                    }
                }
                thiz.PDFAnnotEnd();
            }
            public void OnTouchMove(Point point)
            {
                PDFReader thiz = m_reader;

                float dx = (float)(thiz.m_shold_x - thiz.m_hold_x);
                float dy = (float)(thiz.m_shold_y - thiz.m_hold_y);
                RDRect rect = m_reader.m_annot_rect;
                RDPoint pt1 = m_pt10;
                RDPoint pt2 = m_pt20;
                if (m_node > 0)//select node
                {
                    switch (m_node)
                    {
                        case 1:
                            pt1.x += dx;
                            pt1.y += dy;
                            if (pt1.x > pt2.x)
                            {
                                rect.left = pt2.x;
                                rect.right = pt1.x;
                            }
                            else
                            {
                                rect.left = pt1.x;
                                rect.right = pt2.x;
                            }
                            if (pt1.y > pt2.y)
                            {
                                rect.top = pt2.y;
                                rect.bottom = pt1.y;
                            }
                            else
                            {
                                rect.top = pt1.y;
                                rect.bottom = pt2.y;
                            }
                            break;
                        case 2:
                            pt2.x += dx;
                            pt2.y += dy;
                            if (pt1.x > pt2.x)
                            {
                                rect.left = pt2.x;
                                rect.right = pt1.x;
                            }
                            else
                            {
                                rect.left = pt1.x;
                                rect.right = pt2.x;
                            }
                            if (pt1.y > pt2.y)
                            {
                                rect.top = pt2.y;
                                rect.bottom = pt1.y;
                            }
                            else
                            {
                                rect.top = pt1.y;
                                rect.bottom = pt2.y;
                            }
                            break;
                        default:
                            pt1.x += dx;
                            pt1.y += dy;
                            pt2.x += dx;
                            pt2.y += dy;
                            rect.left += dx;
                            rect.top += dy;
                            rect.right += dx;
                            rect.bottom += dy;
                            break;
                    }
                }
                m_pt1.x = (float)thiz.to_canvasx(pt1.x);
                m_pt1.y = (float)thiz.to_canvasy(pt1.y);
                m_pt2.x = (float)thiz.to_canvasx(pt2.x);
                m_pt2.y = (float)thiz.to_canvasy(pt2.y);
                m_rect.left = (float)thiz.to_canvasx(rect.left);
                m_rect.top = (float)thiz.to_canvasy(rect.top);
                m_rect.right = (float)thiz.to_canvasx(rect.right);
                m_rect.bottom = (float)thiz.to_canvasy(rect.bottom);
            }
        }
        private class AnnotOpPoly : IAnnoitOP
        {
            private PDFReader m_reader;
            private bool m_attached;
            private RDRect m_rect;
            private RDPoint[] m_pts0;
            private RDPoint[] m_pts;
            private int m_node;
            private int m_atype;
            public AnnotOpPoly(PDFReader reader)
            {
                m_reader = reader;
                m_node = -1;
                m_attached = false;
            }
            private RDPoint[] getPoints()
            {
                PDFReader thiz = m_reader;
                m_atype = thiz.m_annot.Type;
                RDPath path = null;
                int cnt = 0;
                if (m_atype == 7)
                {
                    path = thiz.m_annot.PolygonPath;
                    cnt = path.NodesCnt - 1;
                }
                else if (m_atype == 8)
                {
                    path = thiz.m_annot.PolylinePath;
                    cnt = path.NodesCnt;
                }
                if (path == null) return null;
                long vpage = thiz.m_content.vGetPage(thiz.m_annot_pos.pageno);
                RDPoint[] pts = new RDPoint[cnt];
                RDPoint pt1;
                for (int ipt = 0; ipt < cnt; ipt++)
                {
                    pt1 = path.GetPoint(ipt);
                    pt1.x = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, pt1.x));
                    pt1.y = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, pt1.y));
                    float tmp = (float)thiz.m_scroller.HorizontalOffset;
                    pt1.x -= tmp;
                    tmp = (float)thiz.m_scroller.VerticalOffset;
                    pt1.y -= tmp;
                    pts[ipt] = pt1;
                }
                return pts;
            }
            private void attach()
            {
                PDFReader thiz = m_reader;
                if (!m_attached)
                {
                    float dx = (float)(thiz.m_shold_x - thiz.m_hold_x);
                    float dy = (float)(thiz.m_shold_y - thiz.m_hold_y);
                    m_rect = thiz.m_annot_rect;
                    m_rect.left += dx;
                    m_rect.top += dy;
                    m_rect.right += dx;
                    m_rect.bottom += dy;
                    m_rect.left = (float)thiz.to_canvasx(m_rect.left);
                    m_rect.top = (float)thiz.to_canvasy(m_rect.top);
                    m_rect.right = (float)thiz.to_canvasx(m_rect.right);
                    m_rect.bottom = (float)thiz.to_canvasy(m_rect.bottom);
                    m_pts = getPoints();
                    m_pts0 = new RDPoint[m_pts.Length];
                    Array.Copy(m_pts, 0, m_pts0, 0, m_pts.Length);
                    m_attached = true;
                }
            }
            private int getNode(float x, float y)
            {
                int cnt = m_pts.Length;
                for (int ipt = 0; ipt < cnt; ipt++)
                {
                    if (vOnBlk(x, y, m_pts[ipt])) return ipt;
                }
                return -1;
            }
            private RDRect updateRect()
            {
                RDRect rect;
                PDFReader thiz = m_reader;
                int cnt = m_pts.Length;
                rect.left = m_pts[0].x;
                rect.top = m_pts[0].y;
                rect.right = m_pts[0].x;
                rect.bottom = m_pts[0].y;
                for (int ipt = 1; ipt < cnt; ipt++)
                {
                    if (rect.left > m_pts[ipt].x) rect.left = m_pts[ipt].x;
                    else if (rect.right < m_pts[ipt].x) rect.right = m_pts[ipt].x;
                    if (rect.top > m_pts[ipt].y) rect.top = m_pts[ipt].y;
                    else if (rect.bottom < m_pts[ipt].y) rect.bottom = m_pts[ipt].y;
                }
                return rect;
            }
            private RDPath getPath(RDMatrix mat)
            {
                int cnt = m_pts.Length;
                RDPath path = new RDPath();
                path.MoveTo(m_pts[0].x, m_pts[0].y);
                for (int ipt = 1; ipt < cnt; ipt++)
                    path.LineTo(m_pts[ipt].x, m_pts[ipt].y);
                mat.TransformPath(path);
                return path;
            }
            public void OnDraw()
            {
                attach();
                PDFReader thiz = m_reader;
                thiz.m_canvas.draw_rect(m_rect, 1, 0xFF000000);
                if (m_pts != null)
                    thiz.m_canvas.draw_lines_and_blks(m_pts, m_pts.Length, 2, m_blk_gap, 0x400000FF);
            }
            public void OnTouchBeg(Point point)
            {
                attach();
                PDFReader thiz = m_reader;

                m_node = getNode((float)point.X, (float)point.Y);

                if (m_node < 0)
                {
                    m_pts = null;
                    m_pts0 = null;
                    RDRect rect = thiz.m_annot_rect;
                    rect.left = (float)thiz.to_canvasx(rect.left);
                    rect.top = (float)thiz.to_canvasy(rect.top);
                    rect.right = (float)thiz.to_canvasx(rect.right);
                    rect.bottom = (float)thiz.to_canvasy(rect.bottom);
                    if (vInRect(rect, point)) m_node = 0;
                    else m_node = -1;
                }
            }
            public void OnTouchEnd(Point point)
            {
                PDFReader thiz = m_reader;
                m_attached = false;
                thiz.m_modified = true;
                float dx = (float)(point.X - thiz.m_hold_x);
                float dy = (float)(point.Y - thiz.m_hold_y);
                PDFPos pos = thiz.m_content.vGetPos(point.X, point.Y);
                long vpage = thiz.m_content.vGetPage(thiz.m_annot_pos.pageno);
                if (m_pts != null)//select node
                {
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                    RDPath path = getPath(mat);
                    if (m_atype == 7)
                    {
                        path.Close();
                        thiz.m_annot.PolygonPath = path;
                    }
                    else if (m_atype == 8)
                        thiz.m_annot.PolylinePath = path;
                    thiz.m_content.vRenderPage(vpage);
                    if (thiz.m_listener != null) thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                }
                else
                {
                    if (m_node < 0)//invalidate
                    {
                        thiz.PDFAnnotEnd();
                        return;
                    }
                    thiz.m_annot_rect.left += dx;
                    thiz.m_annot_rect.top += dy;
                    thiz.m_annot_rect.right += dx;
                    thiz.m_annot_rect.bottom += dy;
                    if (pos.pageno == thiz.m_annot_pos.pageno)
                    {
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                        thiz.m_annot_rect = mat.TransformRect(thiz.m_annot_rect);
                        RDRect rect = thiz.m_annot.Rect;
                        thiz.m_annot.Rect = thiz.m_annot_rect;
                        thiz.m_opstack.push(PDFOPStack.new_move(pos.pageno, rect, pos.pageno, thiz.m_annot.IndexInPage, thiz.m_annot_rect));
                        thiz.m_content.vRenderPage(vpage);
                        if (thiz.m_listener != null) thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                    }
                    else
                    {
                        long vdest = thiz.m_content.vGetPage(pos.pageno);
                        PDFPage dpage = thiz.m_doc.GetPage(pos.pageno);
                        if (dpage != null)
                        {
                            RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vdest, thiz.m_scroller.HorizontalOffset, thiz.m_scroller.VerticalOffset);
                            RDRect rect = thiz.m_annot.Rect;
                            thiz.m_annot_rect = mat.TransformRect(thiz.m_annot_rect);
                            thiz.m_opstack.push(PDFOPStack.new_move(thiz.m_annot_pos.pageno, rect, pos.pageno, dpage.AnnotCount, thiz.m_annot_rect));
                            dpage.ObjsStart();
                            thiz.m_annot.MoveToPage(dpage, thiz.m_annot_rect);
                            dpage.Close();
                            thiz.m_content.vRenderPage(vpage);
                            thiz.m_content.vRenderPage(vdest);
                            if (thiz.m_listener != null)
                            {
                                thiz.m_listener.OnPDFPageUpdated(pos.pageno);
                                thiz.m_listener.OnPDFPageUpdated(thiz.m_annot_pos.pageno);
                            }
                        }
                    }
                }
                thiz.PDFAnnotEnd();
            }
            public void OnTouchMove(Point point)
            {
                PDFReader thiz = m_reader;

                float dx = (float)(thiz.m_shold_x - thiz.m_hold_x);
                float dy = (float)(thiz.m_shold_y - thiz.m_hold_y);
                RDRect rect;
                if (m_pts != null)
                {
                    m_pts[m_node].x = m_pts0[m_node].x + dx;
                    m_pts[m_node].y = m_pts0[m_node].y + dy;
                    rect = updateRect();
                    m_pts[m_node].x = (float)thiz.to_canvasx(m_pts[m_node].x);
                    m_pts[m_node].y = (float)thiz.to_canvasy(m_pts[m_node].y);
                }
                else
                {
                    rect = m_reader.m_annot_rect;
                    rect.left += dx;
                    rect.top += dy;
                    rect.right += dx;
                    rect.bottom += dy;
                }
                m_rect.left = (float)thiz.to_canvasx(rect.left);
                m_rect.top = (float)thiz.to_canvasy(rect.top);
                m_rect.right = (float)thiz.to_canvasx(rect.right);
                m_rect.bottom = (float)thiz.to_canvasy(rect.bottom);
            }
        }
        private AnnotOpNormal m_aop_normal;
        private AnnotOpResize m_aop_resize;
        private AnnotOpLine m_aop_line;
        private AnnotOpPoly m_aop_poly;
        private IAnnoitOP m_aop;

        static float sm_ovalWidth = 2;
        static uint sm_textColor = 0xFF000000;
        static uint sm_ovalColor = 0xFF0000FF;
        static float sm_rectWidth = 2;
        static uint sm_rectColor = 0xFFFF0000;
        static float sm_inkWidth = 3;
        static uint sm_inkColor = 0xFFFF0000;
        static float sm_lineWidth = 2;
        static uint sm_lineColor = 0xFFFF0000;
        static uint sm_lineColorFill = 0xFF0000FF;
        static RDLAYOUT_MODE sm_viewMode = RDLAYOUT_MODE.layout_vert;
        static public float inkWidth
        {
            get { return sm_inkWidth; }
            set { sm_inkWidth = value; }
        }
        static public uint inkColor
        {
            get { return sm_inkColor; }
            set { sm_inkColor = value; }
        }
        static public float rectWidth
        {
            get { return sm_rectWidth; }
            set { sm_rectWidth = value; }
        }
        static public uint rectColor
        {
            get { return sm_rectColor; }
            set { sm_rectColor = value; }
        }
        static public float ovalWidth
        {
            get { return sm_ovalWidth; }
            set { sm_ovalWidth = value; }
        }
        static public uint ovalColor
        {
            get { return sm_ovalColor; }
            set { sm_ovalColor = value; }
        }
        static public float lineWidth
        {
            get { return sm_lineWidth; }
            set { sm_lineWidth = value; }
        }
        static public uint lineColor
        {
            get { return sm_lineColor; }
            set { sm_lineColor = value; }
        }
        static public uint textColor
        {
            get { return sm_textColor; }
            set { sm_textColor = value; }
        }
        static public RDLAYOUT_MODE viewMode
        {
            get { return sm_viewMode; }
            set { sm_viewMode = value; }
        }
        private enum PDFV_STATUS
        {
            STA_NONE = 0,
            STA_ZOOM = 1,
            STA_SELECT = 2,
            STA_ANNOT = 3,
            STA_NOTE = 4,
            STA_INK = 5,
            STA_RECT = 6,
            STA_ELLIPSE = 7,
            STA_LINE = 8,
            STA_STAMP = 9,
            STA_TEXT_EDIT = 10,
            STA_POLYGON = 11,
            STA_POLYLINE = 12,
            STA_FIELD_CHECK = 20,
            STA_FIELD_RADIO = 21,
            STA_FIELD_SIGNATURE = 22,
            STA_FIELD_TEXT_EDIT = 23,
            STA_EDIT_CONTENT = 100,
        }
        private PDFV_STATUS m_status;

        private double m_oldZoom;
        private int m_pageno;
        private PDFPos m_goto_pos;
        private double m_scale;
        private bool m_keepauto;
        private PDFDoc m_doc;
        private ScrollViewer m_scroller;
        private PDFOPStack m_opstack;
        private PDFVContent m_content;
        private PDFVCanvas m_canvas;//draw some temp elements, over ScrollViewer and layout view.
        class RDControl : Control
        {
        }
        private RDControl m_ctrl;
        private RelativePanel m_parent;
        private RDLAYOUT_MODE m_cur_mode;
        private IPDFViewListener m_listener;
        private RDVSel m_sel;

        private bool m_modified;
        private bool m_autofit;
        private bool m_touched;
        private double m_hold_x;
        private double m_hold_y;
        private double m_shold_x;
        private double m_shold_y;
        private RDRect m_annot_rect;
        private RDRect m_annot_rect0;
        private PDFPos m_annot_pos;
        private PDFAnnot m_annot;
        private PDFPage m_annot_page;
        private long m_annot_vpage;
        /*
        private static byte[] ms_tiny_bmp = new byte[102]
        {
            0x42, 0x4D, 0x66, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00, 0x28, 0x00,
	        0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x01, 0x00, 0x18, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
	        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
	        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
	        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF,
	        0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF
        };
        */
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
            if (m_listener != null) m_listener.OnPDFFound(found);
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
            //double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            //if (tdx >= m_parent.ActualWidth)
            //  return y;
            //else
            if (tdy < m_canvas.ActualHeight)
                return y + (m_canvas.ActualHeight - tdy) * 0.5;
            else
                return y;
        }
        private double to_contx(double x)
        {
            double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            double caw = m_canvas.ActualWidth;
            if (tdx < caw)
                return x - (caw - tdx) * 0.5;
            else
                return x;
        }
        private double to_conty(double y)
        {
            double tdy = m_content.ActualHeight * m_scroller.ZoomFactor;
            //double tdx = m_content.ActualWidth * m_scroller.ZoomFactor;
            //if (tdx >= m_parent.ActualWidth)
            //  return y;
            //else
            if (tdy < m_canvas.ActualHeight)
                return y - (m_canvas.ActualHeight - tdy) * 0.5;
            else
                return y;
        }
        private void vDrawAnnot()
        {
            if (m_status == PDFV_STATUS.STA_ANNOT)
                m_aop.OnDraw();
        }
        private int m_ink_pos;
        private void vDrawInk()
        {
            if (m_status == PDFV_STATUS.STA_INK && m_ink != null)
            {
                m_ink_pos = m_canvas.draw_ink(m_ink, m_ink_path, m_ink_pos, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawPolygon()
        {
            if (m_status == PDFV_STATUS.STA_POLYGON && m_polygon != null)
            {
                m_ink_pos = m_canvas.draw_polygon(m_polygon, m_ink_path, m_ink_pos, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawPolyline()
        {
            if (m_status == PDFV_STATUS.STA_POLYLINE && m_polygon != null)
            {
                m_ink_pos = m_canvas.draw_polyline(m_polygon, m_ink_path, m_ink_pos, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawRects()
        {
            if (m_status == PDFV_STATUS.STA_RECT && m_rects_cnt > 0)
            {
                m_canvas.draw_rects(m_rects, m_rects_cnt, sm_rectWidth, sm_rectColor, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawStamp()
        {
            if (m_status == PDFV_STATUS.STA_STAMP && m_rects_cnt > 0)
            {
                m_canvas.draw_stamps(m_rects, m_rects_cnt, m_stamp_bmp, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawTextEditRects()
        {
            if (m_status == PDFV_STATUS.STA_TEXT_EDIT && m_rects_cnt > 0)
            {
                m_canvas.draw_rects(m_rects, m_rects_cnt, 2, 0xFF0000FF, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawEllipse()
        {
            if (m_status == PDFV_STATUS.STA_ELLIPSE && m_rects_cnt > 0)
            {
                m_canvas.draw_ovals(m_rects, m_rects_cnt, sm_ovalWidth, sm_ovalColor, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawLines()
        {
            if (m_status == PDFV_STATUS.STA_LINE && m_rects_cnt > 0)
            {
                m_canvas.draw_lines(m_rects, m_rects_cnt, sm_lineWidth, sm_lineColor, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawField()
        {
            if ((m_status == PDFV_STATUS.STA_FIELD_TEXT_EDIT || m_status == PDFV_STATUS.STA_FIELD_CHECK || m_status == PDFV_STATUS.STA_FIELD_RADIO || m_status == PDFV_STATUS.STA_FIELD_SIGNATURE) && m_rects_cnt > 0)
            {
                m_canvas.draw_rects(m_rects, m_rects_cnt, 2, 0xFF0000FF, (float)to_canvasx(0), (float)to_canvasy(0));
            }
        }
        private void vDrawPageEdit()
        {
            if (m_status != PDFV_STATUS.STA_EDIT_CONTENT || m_edit_node == null) return;
            m_canvas.draw_rect(m_annot_rect, 2, 0x80000000);

            long pos0;
            long pos1;
            if (m_edit_pos0 > m_edit_pos1)
            {
                pos0 = m_edit_pos1;
                pos1 = m_edit_pos0;
            }
            else
            {
                pos0 = m_edit_pos0;
                pos1 = m_edit_pos1;
            }
            if (!PDFEditNode.caret_is_same(pos0, pos1))
            {
                pos1 = PDFEditNode.caret_regular_end(pos1);
                pos0 = m_edit_node.caret_regular_start(pos0);
                RDRect rect_word = m_edit_node.GetCharRect(pos0);
                pos0 = m_edit_node.GetCharNext(pos0);
                long posn = PDFEditNode.caret_regular_end(pos0);

                RDRect dr;
                while (posn < pos1)
                {
                    RDRect rect_char = m_edit_node.GetCharRect(pos0);
                    if (PDFEditNode.caret_is_vert(pos0))
                    {
                        float minr = (rect_char.right < rect_word.right) ? rect_char.right : rect_word.right;
                        float maxl = (rect_char.left > rect_word.left) ? rect_char.left : rect_word.left;
                        float dx = minr - maxl;
                        if (dx * 1.25f > rect_char.right - rect_char.left && dx * 1.25f > rect_word.right - rect_word.left)
                        {
                            if (rect_char.left < rect_word.left) rect_word.left = rect_char.left;
                            if (rect_char.right > rect_word.right) rect_word.right = rect_char.right;
                            if (rect_char.top < rect_word.top) rect_word.top = rect_char.top;
                            if (rect_char.bottom > rect_word.bottom) rect_word.bottom = rect_char.bottom;
                        }
                        else
                        {
                            dr.left = (float)(PDFVContent.pgGetVX(m_annot_vpage, rect_word.left) - m_content.vGetX());
                            dr.right = (float)(PDFVContent.pgGetVX(m_annot_vpage, rect_word.right) - m_content.vGetX());
                            dr.top = (float)(PDFVContent.pgGetVY(m_annot_vpage, rect_word.bottom) - m_content.vGetY());
                            dr.bottom = (float)(PDFVContent.pgGetVY(m_annot_vpage, rect_word.top) - m_content.vGetY());
                            m_canvas.fill_rect(dr, Color.FromArgb(0x40, 0, 0, 255));
                            rect_word = rect_char;
                        }
                    }
                    else
                    {
                        float minb = (rect_char.bottom < rect_word.bottom) ? rect_char.bottom : rect_word.bottom;
                        float maxt = (rect_char.top > rect_word.top) ? rect_char.top : rect_word.top;
                        float dy = minb - maxt;
                        if (dy * 1.25f > rect_char.bottom - rect_char.top && dy * 1.25f > rect_word.bottom - rect_word.top)
                        {
                            if (rect_char.left < rect_word.left) rect_word.left = rect_char.left;
                            if (rect_char.right > rect_word.right) rect_word.right = rect_char.right;
                            if (rect_char.top < rect_word.top) rect_word.top = rect_char.top;
                            if (rect_char.bottom > rect_word.bottom) rect_word.bottom = rect_char.bottom;
                        }
                        else
                        {
                            dr.left = (float)(PDFVContent.pgGetVX(m_annot_vpage, rect_word.left) - m_content.vGetX());
                            dr.right = (float)(PDFVContent.pgGetVX(m_annot_vpage, rect_word.right) - m_content.vGetX());
                            dr.top = (float)(PDFVContent.pgGetVY(m_annot_vpage, rect_word.bottom) - m_content.vGetY());
                            dr.bottom = (float)(PDFVContent.pgGetVY(m_annot_vpage, rect_word.top) - m_content.vGetY());
                            m_canvas.fill_rect(dr, Color.FromArgb(0x40, 0, 0, 255));
                            rect_word = rect_char;
                        }
                    }
                    pos0 = m_edit_node.GetCharNext(pos0);
                    posn = PDFEditNode.caret_regular_end(pos0);
                }
                dr.left = (float)(PDFVContent.pgGetVX(m_annot_vpage, rect_word.left) - m_content.vGetX());
                dr.right = (float)(PDFVContent.pgGetVX(m_annot_vpage, rect_word.right) - m_content.vGetX());
                dr.top = (float)(PDFVContent.pgGetVY(m_annot_vpage, rect_word.bottom) - m_content.vGetY());
                dr.bottom = (float)(PDFVContent.pgGetVY(m_annot_vpage, rect_word.top) - m_content.vGetY());
                m_canvas.fill_rect(dr, Color.FromArgb(0x40, 0, 0, 255));
            }
            if ((m_edit_caret_sta & 1) != 0)
            {
                if ((m_edit_caret_sta & 2) != 0 && m_edit_caret_rect.left != m_edit_caret_rect.right)
                    m_canvas.fill_rect(m_edit_caret_rect, Color.FromArgb(0xC0, 0, 0, 0));
            }
        }
        private void vDraw()
        {
            if (m_content == null) return;
            m_canvas.clear();
            vDrawAnnot();
            vDrawInk();
            vDrawEllipse();
            vDrawRects();
            vDrawTextEditRects();
            vDrawLines();
            vDrawStamp();
            vDrawPolygon();
            vDrawPolyline();

            vDrawField();
            vDrawPageEdit();
            m_content.vDraw(m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
            m_content.vDrawFind(to_contx(m_scroller.HorizontalOffset), to_conty(m_scroller.VerticalOffset));
            DrawFTS();
            if (m_listener != null && m_status != PDFV_STATUS.STA_ZOOM)
            {
                int pageno = m_content.vGetPage(m_scroller.ActualWidth * 0.25, m_scroller.ActualHeight * 0.25);
                if (pageno != m_pageno)
                {
                    m_pageno = pageno;
                    m_listener.OnPDFPageChanged(m_pageno);
                }
                double scale = m_content.vGetScale();
                if (scale != m_scale)
                {
                    m_scale = scale;
                    m_listener.OnPDFScaleChanged(m_scale);
                }
            }
            if (m_sel != null) m_sel.DrawSel(m_content, m_content.vGetPage(m_sel.GetPageNo()));
        }
        public Boolean PDFModified
        {
            get { return m_modified; }
            set { m_modified = value; }
        }
        public Boolean PDFAutoFit
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
                        PDFReader viewer = this;
                        //m_scroller.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler([viewer, pageno]() { viewer.PDFGotoPage(pageno); }));
                        m_scroller.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { viewer.PDFGotoPage(pageno); });
                        m_goto_pos.pageno = -1;
                        m_goto_pos.x = 0;
                        m_goto_pos.y = 0;
                    }
                }
            }
        }
        public PDFReader()
        {
            m_parent = null;
            m_pageno = -1;
            m_scale = -1;

            m_doc = null;
            m_oldZoom = -1;
            m_touched = false;
            m_modified = false;
            m_autofit = true;
            m_rects_cnt = 0;
            m_status = PDFV_STATUS.STA_NONE;
            m_sel = null;
            m_cur_mode = RDLAYOUT_MODE.layout_unknown;
            m_goto_pos.pageno = -1;
            m_goto_pos.x = 0;
            m_goto_pos.y = 0;
            m_keepauto = false;
            m_opstack = new PDFOPStack();
            m_aop_normal = new AnnotOpNormal(this);
            m_aop_resize = new AnnotOpResize(this);
            m_aop_line = new AnnotOpLine(this);
            m_aop_poly = new AnnotOpPoly(this);
        }
        ~PDFReader()
        {
            PDFClose();
        }
        public bool PDFOpen(RelativePanel parent, PDFDoc doc, RDLAYOUT_MODE lmode, IPDFViewListener listener)
        {
            if (parent == null || doc == null) return false;
            load_stamp_bmp();

            m_parent = parent;
            m_scroller = new ScrollViewer();
            m_content = new PDFVContent();
            m_canvas = new PDFVCanvas();
            m_ctrl = new RDControl();
            m_parent.Children.Add(m_ctrl);
            m_ctrl.Width = 1;
            m_ctrl.Height = 1;
            m_ctrl.SetValue(RelativePanel.AlignLeftWithPanelProperty, true);
            m_ctrl.SetValue(RelativePanel.AlignTopWithPanelProperty, true);
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
            m_ctrl.KeyDown += vOnKeyDown;
            m_ctrl.CharacterReceived += vOnChar;

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
                PDFAutoFit = val;
                vDraw();
            }
            else m_content.vSetAutoFit(m_autofit);
            return true;
        }
        private PDFPos m_save_pos;
        public void PDFSaveView()
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
            m_opstack = null;

            m_parent.PointerPressed -= vOnTouchDown;
            m_parent.PointerMoved -= vOnTouchMove;
            m_parent.PointerReleased -= vOnTouchUp;
            m_parent.PointerCanceled -= vOnTouchUp;
            m_parent.PointerExited -= vOnTouchUp;
            m_parent.Tapped -= vOnTapped;
            m_parent.DoubleTapped -= vOnDoubleTapped;
        }
        public void PDFRestoreView()
        {
            m_content = new PDFVContent();
            m_canvas = new PDFVCanvas();
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
                PDFAutoFit = val;
                if (m_save_pos.pageno >= m_doc.PageCount)
                    m_save_pos.pageno = m_doc.PageCount - 1;
                m_content.vResize(m_scroller.ActualWidth, m_scroller.ActualHeight, m_scroller.ZoomFactor);
                m_content.vSetPos(m_scroller.ActualWidth * 0.5, m_scroller.ActualHeight * 0.5, m_save_pos);
                vDraw();
            }
            else m_content.vSetAutoFit(m_autofit);
        }
        public RDLAYOUT_MODE PDFViewMode
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
                PDFPos pos = m_content.vGetPos(vw * 0.5, vh * 0.5);
                m_scroller.ZoomToFactor(1);
                m_scale = 1;
                //m_scroller.ChangeView(0, 0, 1);
                m_content.vSetView(value);
                if (vw > 0 && vh > 0) m_content.vResize(vw, vh, m_scroller.ZoomFactor);
                m_content.vSetPos(vx, vy, pos);//will update scroll view.
                m_save_pos = pos;
                m_content.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { vDraw(); });
            }
        }
        public void PDFClose()
        {
            if (m_doc == null) return;
            if (m_annot_page != null)
            {
                m_annot_page.Close();
                m_annot_page = null;
            }

            caret_show(false);
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
            m_modified = false;
            m_rects_cnt = 0;
            m_sel = null;
            m_cur_mode = RDLAYOUT_MODE.layout_unknown;
            m_opstack = null;

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
        public int PDFGetCurPageNo()
        {
            return m_pageno;
        }
        public PDFPos PDFGetCurPos()
        {
            if (m_content == null)
            {
                PDFPos ret;
                ret.pageno = -1;
                ret.x = 0;
                ret.y = 0;
                return ret;
            }
            return m_content.vGetPos(m_scroller.ActualWidth * 0.25, m_scroller.ActualHeight * 0.25);
        }
        public float PDFGetScale()
        {
            if (m_content == null) return -1;
            return (float)m_content.vGetScale();
        }
        public void PDFGotoPage(int pageno)
        {
            if (m_doc == null) return;
            if (m_content != null && m_content.vCanSetPos())
            {
                PDFPos pos;
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
                PDFAutoFit = true;
            }
            else
            {
                m_content.vResize(e.NewSize.Width, e.NewSize.Height, m_scroller.ZoomFactor);
                if (m_goto_pos.pageno >= 0)
                {
                    int pageno = m_goto_pos.pageno;
                    PDFReader viewer = this;
                    //m_scroller.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, new DispatchedHandler([viewer, pageno]() { viewer.PDFGotoPage(pageno); }));
                    m_scroller.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, async () => { viewer.PDFGotoPage(pageno); });
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
                if (m_status == PDFV_STATUS.STA_NONE)
                {
                    m_content.vZoomStart();
                    m_status = PDFV_STATUS.STA_ZOOM;
                }
                if (m_status == PDFV_STATUS.STA_ZOOM)
                {
                    m_content.vZoomSet(m_scroller.ZoomFactor);
                    //m_content.vResize(m_scroller.ActualWidth, m_scroller.ActualHeight, m_scroller.ZoomFactor);
                    if (m_keepauto)
                    {
                        m_content.vSetAutoFit(true);
                        m_keepauto = false;
                        m_content.vZoomConfirm();
                        m_status = PDFV_STATUS.STA_NONE;
                    }
                    else
                        m_autofit = false;
                }
                m_oldZoom = m_scroller.ZoomFactor;
            }
            else
            {
                if (m_status == PDFV_STATUS.STA_ZOOM)
                {
                    m_content.vZoomConfirm();
                    m_status = PDFV_STATUS.STA_NONE;
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
            if (OnAnnotTouchBegin(pt)) return;
            if (OnNoteTouchBegin(pt)) return;
            if (OnInkTouchBegin(pt)) return;
            if (OnRectTouchBegin(pt)) return;
            if (OnEllipseTouchBegin(pt)) return;
            if (OnLineTouchBegin(pt)) return;
            if (OnStampTouchBegin(pt)) return;
            if (OnEditTextBoxTouchBegin(pt)) return;
            if (OnPolygonTouchBegin(pt)) return;
            if (OnPolylineTouchBegin(pt)) return;

            if (OnFieldCheckTouchBegin(pt)) return;
            if (OnFieldRadioTouchBegin(pt)) return;
            if (OnFieldSignTouchBegin(pt)) return;
            if (OnFieldEditTextBoxTouchBegin(pt)) return;

            if (OnPageEditBegin(pt)) return;
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
                if (OnAnnotTouchMove(pt)) return;
                if (OnNoteTouchMove(pt)) return;
                if (OnInkTouchMove(pt)) return;
                if (OnRectTouchMove(pt)) return;
                if (OnEllipseTouchMove(pt)) return;
                if (OnLineTouchMove(pt)) return;
                if (OnStampTouchMove(pt)) return;
                if (OnEditTextBoxTouchMove(pt)) return;
                if (OnPolygonTouchMove(pt)) return;
                if (OnPolylineTouchMove(pt)) return;

                if (OnFieldCheckTouchMove(pt)) return;
                if (OnFieldRadioTouchMove(pt)) return;
                if (OnFieldSignTouchMove(pt)) return;
                if (OnFieldEditTextBoxTouchMove(pt)) return;

                if (OnPageEditMove(pt)) return;
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
                if (OnAnnotTouchEnd(pt)) return;
                if (OnNoteTouchEnd(pt)) return;
                if (OnInkTouchEnd(pt)) return;
                if (OnRectTouchEnd(pt)) return;
                if (OnEllipseTouchEnd(pt)) return;
                if (OnLineTouchEnd(pt)) return;
                if (OnStampTouchEnd(pt)) return;
                if (OnEditTextBoxTouchEnd(pt)) return;
                if (OnPolygonTouchEnd(pt)) return;
                if (OnPolylineTouchEnd(pt)) return;

                if (OnFieldCheckTouchEnd(pt)) return;
                if (OnFieldRadioTouchEnd(pt)) return;
                if (OnFieldSignTouchEnd(pt)) return;
                if (OnFieldEditTextBoxTouchEnd(pt)) return;

                if (OnPageEditEnd(pt)) return;
            }
        }
        private void vOnTapped(Object sender, TappedRoutedEventArgs e)
        {
            if (m_status != PDFV_STATUS.STA_NONE) return;
            Point point = e.GetPosition(m_canvas);
            point.X = to_contx(point.X);
            point.Y = to_conty(point.Y);
            m_annot_pos = m_content.vGetPos(point.X, point.Y);
            if (m_annot_pos.pageno >= 0)
            {
                long vpage = m_content.vGetPage(m_annot_pos.pageno);
                if (vpage == 0)//shall not happen
                {
                    if (m_listener != null) m_listener.OnPDFSingleTapped((float)point.X, (float)point.Y);
                    return;
                }
                m_annot_page = m_doc.GetPage(PDFVContent.pgGetPageNo(vpage));
                if (m_annot_page == null)
                {
                    if (m_listener != null)
                    {
                        m_listener.OnPDFPageTapped(m_annot_pos.pageno);
                        m_listener.OnPDFSingleTapped((float)point.X, (float)point.Y);
                    }
                    return;
                }
                m_annot_page.ObjsStart();
                m_annot = m_annot_page.GetAnnot((float)m_annot_pos.x, (float)m_annot_pos.y);
                if (m_annot != null)//enter annotation status.
                {
                    int itmp = m_annot.GetCheckStatus();
                    if (m_doc.CanSave && itmp >= 0)
                    {
                        switch (itmp)
                        {
                            case 0:
                                m_annot.SetCheckValue(true);
                                break;
                            case 1:
                                m_annot.SetCheckValue(false);
                                break;
                            case 2:
                            case 3:
                                m_annot.SetRadio();
                                break;
                        }
                        m_content.vRenderPage(vpage);
                        //m_status still is STA_NONE, PDFAnnotEnd() does nothing.
                        //PDFAnnotEnd();
                        vDraw();
                        m_modified = true;
                        if (m_listener != null) m_listener.OnPDFPageUpdated(m_annot_pos.pageno);
                        return;
                    }
                    m_scroller.IsEnabled = false;
                    m_status = PDFV_STATUS.STA_ANNOT;
                    m_annot_rect = m_annot.Rect;
                    m_annot_rect.left = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, m_annot_rect.left));
                    m_annot_rect.right = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, m_annot_rect.right));
                    float tmp = m_annot_rect.top;
                    m_annot_rect.top = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, m_annot_rect.bottom));
                    m_annot_rect.bottom = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, tmp));

                    tmp = (float)m_scroller.HorizontalOffset;
                    m_annot_rect.left -= tmp;
                    m_annot_rect.right -= tmp;
                    tmp = (float)m_scroller.VerticalOffset;
                    m_annot_rect.top -= tmp;
                    m_annot_rect.bottom -= tmp;

                    m_shold_x = m_hold_x;
                    m_shold_y = m_hold_y;

                    int atype = m_annot.Type;
                    if (atype == 5 || atype == 6 || atype == 15)//it can resize
                        m_aop = m_aop_resize;
                    else if (atype == 4)//it is line annotation
                        m_aop = m_aop_line;
                    else if (atype == 7 || atype == 8)//it is polygon or polyline annotation
                        m_aop = m_aop_poly;
                    else
                        m_aop = m_aop_normal;

                    vDraw();
                    if (m_listener != null)
                    {
                        m_listener.OnPDFPageTapped(PDFVContent.pgGetPageNo(vpage));
                        RDRect rect;
                        rect.left = (float)to_canvasx(m_annot_rect.left);
                        rect.top = (float)to_canvasy(m_annot_rect.top);
                        rect.right = (float)to_canvasx(m_annot_rect.right);
                        rect.bottom = (float)to_canvasy(m_annot_rect.bottom);
                        m_listener.OnPDFAnnotClicked(m_annot_page, m_annot_pos.pageno, m_annot, rect);
                    }
                }
                else
                {
                    m_annot_page.Close();
                    m_annot_page = null;
                    if (m_listener != null)
                    {
                        m_listener.OnPDFPageTapped(PDFVContent.pgGetPageNo(vpage));
                        m_listener.OnPDFSingleTapped((float)point.X, (float)point.Y);
                    }
                }
            }
        }
        private void vOnDoubleTapped(Object sender, DoubleTappedRoutedEventArgs e)
        {
            if (m_status == PDFV_STATUS.STA_NONE)
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
        private void vOnKeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (m_status != PDFV_STATUS.STA_EDIT_CONTENT || m_edit_node == null || m_edit_pos0 == -1) return;
            switch (e.Key)
            {
                case Windows.System.VirtualKey.Delete:
                    m_edit_node.CharDelete(m_edit_pos0, m_edit_pos1);
                    if (m_edit_pos0 > m_edit_pos1)
                        m_edit_pos0 = m_edit_pos1;
                    else
                        m_edit_pos1 = m_edit_pos0;
                    onEditCaret();
                    m_annot_page.SetPGEditorModified(true);
                    m_content.vRenderPage(m_annot_vpage);
                    vDraw();
                    break;
                case Windows.System.VirtualKey.Back:
                    if (!PDFEditNode.caret_is_same(m_edit_pos0, m_edit_pos1))
                    {
                        m_edit_node.CharDelete(m_edit_pos0, m_edit_pos1);
                        if (m_edit_pos0 > m_edit_pos1)
                            m_edit_pos0 = m_edit_pos1;
                        else
                            m_edit_pos1 = m_edit_pos0;
                        onEditCaret();
                        m_annot_page.SetPGEditorModified(true);
                        m_content.vRenderPage(m_annot_vpage);
                        vDraw();
                        return;
                    }
                    if (!PDFEditNode.caret_is_first(m_edit_pos0))
                    {
                        long pos;
                        if (PDFEditNode.caret_is_end(m_edit_pos0))
                        {
                            pos = m_edit_pos0;
                            pos &= (~1l);
                        }
                        else
                            pos = m_edit_node.GetCharPrev(m_edit_pos0);
                        m_edit_node.CharDelete(pos, pos);
                        m_edit_pos0 = pos;
                        m_edit_pos1 = pos;
                        onEditCaret();
                        m_annot_page.SetPGEditorModified(true);
                        m_content.vRenderPage(m_annot_vpage);
                        vDraw();
                    }
                    break;
                case Windows.System.VirtualKey.Enter:
                    if (!PDFEditNode.caret_is_same(m_edit_pos0, m_edit_pos1))
                    {
                        m_edit_node.CharDelete(m_edit_pos0, m_edit_pos1);
                        if (m_edit_pos0 > m_edit_pos1)
                            m_edit_pos0 = m_edit_pos1;
                        else
                            m_edit_pos1 = m_edit_pos0;
                    }
                    m_edit_node.CharReturn(m_edit_pos0);
                    m_edit_pos0 = m_edit_node.GetCharNext(m_edit_pos0);
                    m_edit_pos1 = m_edit_pos0;
                    onEditCaret();
                    m_annot_page.SetPGEditorModified(true);
                    m_content.vRenderPage(m_annot_vpage);
                    vDraw();
                    break;
                case Windows.System.VirtualKey.Left:
                    if (PDFEditNode.caret_is_vert(m_edit_pos0))
                    {
                        RDRect rect = m_edit_node.GetCharRect(m_edit_pos0);
                        if (PDFEditNode.caret_is_end(m_edit_pos1))
                            m_edit_pos1 = m_edit_node.GetCharNextLine(rect.top, m_edit_pos1);
                        else
                            m_edit_pos1 = m_edit_node.GetCharNextLine(rect.bottom, m_edit_pos1);
                    }
                    else
                        m_edit_pos1 = m_edit_node.GetCharPrev(m_edit_pos1);
                    m_edit_pos0 = m_edit_pos1;
                    onEditCaret();
                    vDraw();
                    break;
                case Windows.System.VirtualKey.Right:
                    if (PDFEditNode.caret_is_vert(m_edit_pos0))
                    {
                        RDRect rect = m_edit_node.GetCharRect(m_edit_pos0);
                        if (PDFEditNode.caret_is_end(m_edit_pos1))
                            m_edit_pos1 = m_edit_node.GetCharPrevLine(rect.top, m_edit_pos1);
                        else
                            m_edit_pos1 = m_edit_node.GetCharPrevLine(rect.bottom, m_edit_pos1);
                    }
                    else
                        m_edit_pos1 = m_edit_node.GetCharNext(m_edit_pos1);
                    m_edit_pos0 = m_edit_pos1;
                    onEditCaret();
                    vDraw();
                    break;
                case Windows.System.VirtualKey.Up:
                    if (PDFEditNode.caret_is_vert(m_edit_pos0))
                        m_edit_pos1 = m_edit_node.GetCharPrev(m_edit_pos1);
                    else
                    {
                        RDRect rect = m_edit_node.GetCharRect(m_edit_pos0);
                        if (PDFEditNode.caret_is_end(m_edit_pos1))
                            m_edit_pos1 = m_edit_node.GetCharPrevLine(rect.top, m_edit_pos1);
                        else
                            m_edit_pos1 = m_edit_node.GetCharPrevLine(rect.bottom, m_edit_pos1);
                    }
                    m_edit_pos0 = m_edit_pos1;
                    onEditCaret();
                    vDraw();
                    break;
                case Windows.System.VirtualKey.Down:
                    if (PDFEditNode.caret_is_vert(m_edit_pos0))
                        m_edit_pos1 = m_edit_node.GetCharNext(m_edit_pos1);
                    else
                    {
                        RDRect rect = m_edit_node.GetCharRect(m_edit_pos0);
                        if (PDFEditNode.caret_is_end(m_edit_pos1))
                            m_edit_pos1 = m_edit_node.GetCharNextLine(rect.top, m_edit_pos1);
                        else
                            m_edit_pos1 = m_edit_node.GetCharNextLine(rect.bottom, m_edit_pos1);
                    }
                    m_edit_pos0 = m_edit_pos1;
                    onEditCaret();
                    vDraw();
                    break;
                case Windows.System.VirtualKey.Home:
                    m_edit_pos0 = m_edit_node.CharHome(m_edit_pos1);
                    m_edit_pos1 = m_edit_pos0;
                    onEditCaret();
                    vDraw();
                    break;
                case Windows.System.VirtualKey.End:
                    m_edit_pos0 = m_edit_node.CharEnd(m_edit_pos1);
                    m_edit_pos1 = m_edit_pos0;
                    onEditCaret();
                    vDraw();
                    break;
            }
        }
        private void vOnChar(UIElement sendor, CharacterReceivedRoutedEventArgs e)
        {
            if (m_status != PDFV_STATUS.STA_EDIT_CONTENT || m_edit_node == null || m_edit_pos0 == -1) return;
            if (Char.IsControl(e.Character)) return;
            String sval = e.Character.ToString();
            if (!PDFEditNode.caret_is_same(m_edit_pos0, m_edit_pos1))
            {
                m_edit_node.CharDelete(m_edit_pos0, m_edit_pos1);
                if (m_edit_pos0 > m_edit_pos1)
                    m_edit_pos0 = m_edit_pos1;
                else
                    m_edit_pos1 = m_edit_pos0;
            }
            m_edit_pos0 = m_edit_node.CharInsert(m_edit_pos0, sval);
            m_edit_pos1 = m_edit_pos0;
            onEditCaret();
            m_annot_page.SetPGEditorModified(true);
            m_content.vRenderPage(m_annot_vpage);
            vDraw();
        }
        public void PDFRenderPage(int pageno)
        {
            m_content.vRenderPage(m_content.vGetPage(pageno));
        }
        public void PDFUndo()
        {
            IPDFOPItem op = m_opstack.undo();
            if (op == null) return;
            op.undo(m_doc);
            int pg0 = op.get_pgno(0);
            int pg1 = op.get_pgno(1);
            PDFGotoPage(op.get_cur());
            m_content.vRenderPage(m_content.vGetPage(pg0));
            if (pg0 != pg1)
            {
                m_content.vRenderPage(m_content.vGetPage(pg1));
                if (m_listener != null)
                {
                    m_listener.OnPDFPageUpdated(pg0);
                    m_listener.OnPDFPageUpdated(pg1);
                }
            }
            else if (m_listener != null) m_listener.OnPDFPageUpdated(pg0);
            vDraw();
        }
        public void PDFRedo()
        {
            IPDFOPItem op = m_opstack.redo();
            if (op == null) return;
            op.redo(m_doc);
            int pg0 = op.get_pgno(0);
            int pg1 = op.get_pgno(1);
            PDFGotoPage(op.get_cur());
            m_content.vRenderPage(m_content.vGetPage(pg0));
            if (pg0 != pg1)
            {
                m_content.vRenderPage(m_content.vGetPage(pg1));
                if (m_listener != null)
                {
                    m_listener.OnPDFPageUpdated(pg0);
                    m_listener.OnPDFPageUpdated(pg1);
                }
            }
            else if (m_listener != null) m_listener.OnPDFPageUpdated(pg0);
            vDraw();
        }
        public void PDFSelStart()
        {
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_SELECT;
                m_scroller.IsEnabled = false;
            }
        }
        public void PDFSelEnd()
        {
            if (m_status == PDFV_STATUS.STA_SELECT)
            {
                PDFSelCancel();
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
            }
        }
        public void PDFSelCancel()
        {
            if (m_status == PDFV_STATUS.STA_SELECT)
            {
                m_sel = null;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public String PDFSelGetText()
        {
            if (m_status != PDFV_STATUS.STA_SELECT) return null;
            if (m_sel != null)
                return m_sel.GetSelString();
            return null;
        }
        public bool PDFSelSetMarkup(uint color, int type)
        {
            if (m_status != PDFV_STATUS.STA_SELECT) return false;
            int pageno = m_sel.GetPageNo();
            if (pageno >= 0)
            {
                m_sel.SetSelMarkup(color, type);
                PDFPage page = m_sel.GetPage();
                m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                m_content.vRenderPage(m_content.vGetPage(pageno));
                vDraw();
                m_modified = true;
                if (m_listener != null) m_listener.OnPDFPageUpdated(m_sel.GetPageNo());
                return true;
            }
            return false;
        }
        public bool PDFSelRemove()
        {
            if (!m_sel.EraseSel()) return false;
            m_content.vRenderPage(m_content.vGetPage(m_sel.GetPageNo()));
            vDraw();
            m_modified = true;
            return true;
        }
        public void PDFAnnotPerform()
        {
            if (m_status != PDFV_STATUS.STA_ANNOT) return;
            int pageno = m_annot.Dest;
            if (pageno >= 0)//goto page
            {
                if (m_listener != null)
                    m_listener.OnPDFAnnotGoto(pageno);
                PDFAnnotEnd();
                return;
            }
            if (m_annot.IsRemoteDest)
            {
                if (m_listener != null)
                    m_listener.OnPDFAnnotRemoteDest(m_annot.RemoteDest);
                PDFAnnotEnd();
                return;
            }
            if (m_annot.IsFileLink)
            {
                if (m_listener != null)
                    m_listener.OnPDFAnnotFileLink(m_annot.FileLink);
                PDFAnnotEnd();
                return;
            }
            if (m_annot.IsURI)//open url
            {
                if (m_listener != null)
                    m_listener.OnPDFAnnotURI(m_annot.URI);
                PDFAnnotEnd();
                return;
            }
            if (m_annot.RichMediaItemCount > 0)     // Check if the annotation is a rich media container
            {
                if (m_listener != null)
                    m_listener.OnPDFAnnotRichMedia(m_annot);
                PDFAnnotEnd();
                return;
            }
            String rend = m_annot.GetRenditionName();
            if (rend != null && rend.Length > 0)
            {
                if (m_listener != null)
                    m_listener.OnPDFAnnotRendition(m_annot);
                PDFAnnotEnd();
                return;
            }
            if (m_annot.IsPopup && m_annot.Type == 1) //Nermeen add type check as it enters also for highlight annot
            {
                //popup dialog to show text and subject.
                //nuri is text content.
                //subj is subject string.

                if (m_listener != null)
                    m_listener.OnPDFAnnotPopup(m_annot, m_annot.PopupSubject, m_annot.PopupText);
                PDFAnnotEnd();
                return;
            }
            PDFAnnotEnd();
            return;
        }
        public void PDFAnnotRemove()
        {
            if (m_status != PDFV_STATUS.STA_ANNOT) return;
            PDFPage page = m_doc.GetPage(m_annot_pos.pageno);
            page.ObjsStart();
            m_opstack.push(PDFOPStack.new_del(m_annot_pos.pageno, page, m_annot.IndexInPage));
            if (m_annot.RemoveFromPage())
            {
                m_content.vRenderPage(m_content.vGetPage(m_annot_pos.pageno));
                vDraw();
            }
            page.Close();
            PDFAnnotEnd();
        }
        public void PDFAnnotEnd()
        {
            if (m_status != PDFV_STATUS.STA_ANNOT) return;
            m_status = PDFV_STATUS.STA_NONE;
            m_scroller.IsEnabled = true;
            if (m_listener != null)
                m_listener.OnPDFAnnotEnd();
            m_annot = null;
            if (m_annot_page != null)
            {
                m_annot_page.Close();
                m_annot_page = null;
            }
            vDraw();
        }
        public void PDFUpdateAnnotPage()
        {
            if (m_annot_pos.pageno < 0) return;
            m_content.vRenderPage(m_content.vGetPage(m_annot_pos.pageno));
            vDraw();
        }
        public bool PDFEllipseStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_ELLIPSE;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }
        public void PDFEllipseCancel()
        {
            if (m_status == PDFV_STATUS.STA_ELLIPSE)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public void PDFEllipseEnd()
        {
            if (m_status == PDFV_STATUS.STA_ELLIPSE)
            {
                long[] pages = new long[128];
                int cur;
                int end;
                int pages_cnt = 0;
                int pt_cur = 0;
                int pt_end = m_rects_cnt * 2;
                while (pt_cur < pt_end)
                {
                    RDRect rect;
                    RDPoint pt0 = m_rects[pt_cur];
                    RDPoint pt1 = m_rects[pt_cur + 1];
                    int pageno = m_content.vGetPage(pt0.x, pt0.y);
                    if (pageno >= 0)
                    {
                        long vpage = m_content.vGetPage(pageno);
                        cur = 0;
                        end = pages_cnt;
                        while (cur < end)
                        {
                            if (pages[cur] == vpage) break;
                            cur++;
                        }
                        if (cur >= end)
                        {
                            pages[cur] = vpage;
                            pages_cnt++;
                        }
                        if (pt0.x > pt1.x)
                        {
                            rect.right = pt0.x;
                            rect.left = pt1.x;
                        }
                        else
                        {
                            rect.left = pt0.x;
                            rect.right = pt1.x;
                        }
                        if (pt0.y > pt1.y)
                        {
                            rect.bottom = pt0.y;
                            rect.top = pt1.y;
                        }
                        else
                        {
                            rect.top = pt0.y;
                            rect.bottom = pt1.y;
                        }
                        PDFPage page = m_doc.GetPage(pageno);
                        page.ObjsStart();
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                        rect = mat.TransformRect(rect);
                        page.AddAnnotEllipse(rect, sm_ovalWidth / (float)PDFVContent.pgGetScale(vpage), sm_ovalColor, 0);
                        page.Close();
                    }
                    pt_cur += 2;
                }
                if (m_rects_cnt != 0)
                    m_modified = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;

                cur = 0;
                end = pages_cnt;
                while (cur < end)
                {
                    m_content.vRenderPage(pages[cur]);
                    if (m_listener != null) m_listener.OnPDFPageUpdated(PDFVContent.pgGetPageNo(pages[cur]));
                    cur++;
                }
                vDraw();
                m_scroller.IsEnabled = true;
            }
        }
        public bool PDFRectStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_RECT;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }
        public void PDFRectCancel()
        {
            if (m_status == PDFV_STATUS.STA_RECT)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public void PDFRectEnd()
        {
            if (m_status == PDFV_STATUS.STA_RECT)
            {
                long[] pages = new long[128];
                int cur;
                int end;
                int pages_cnt = 0;
                int pt_cur = 0;
                int pt_end = m_rects_cnt * 2;
                while (pt_cur < pt_end)
                {
                    RDRect rect;
                    RDPoint pt0 = m_rects[pt_cur];
                    RDPoint pt1 = m_rects[pt_cur + 1];
                    int pageno = m_content.vGetPage(pt0.x, pt0.y);
                    if (pageno >= 0)
                    {
                        long vpage = m_content.vGetPage(pageno);
                        cur = 0;
                        end = pages_cnt;
                        while (cur < end)
                        {
                            if (pages[cur] == vpage) break;
                            cur++;
                        }
                        if (cur >= end)
                        {
                            pages[cur] = vpage;
                            pages_cnt++;
                        }
                        if (pt0.x > pt1.x)
                        {
                            rect.right = pt0.x;
                            rect.left = pt1.x;
                        }
                        else
                        {
                            rect.left = pt0.x;
                            rect.right = pt1.x;
                        }
                        if (pt0.y > pt1.y)
                        {
                            rect.bottom = pt0.y;
                            rect.top = pt1.y;
                        }
                        else
                        {
                            rect.top = pt0.y;
                            rect.bottom = pt1.y;
                        }
                        PDFPage page = m_doc.GetPage(pageno);
                        page.ObjsStart();
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                        rect = mat.TransformRect(rect);
                        page.AddAnnotRect(rect, sm_rectWidth / (float)PDFVContent.pgGetScale(vpage), sm_rectColor, 0);
                        m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                        page.Close();
                    }
                    pt_cur += 2;
                }
                if (m_rects_cnt != 0)
                    m_modified = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;

                cur = 0;
                end = pages_cnt;
                while (cur < end)
                {
                    m_content.vRenderPage(pages[cur]);
                    if (m_listener != null) m_listener.OnPDFPageUpdated(PDFVContent.pgGetPageNo(pages[cur]));
                    cur++;
                }
                vDraw();
                m_scroller.IsEnabled = true;
            }
        }
        public bool PDFStampStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_STAMP;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }
        public void PDFStampCancel()
        {
            if (m_status == PDFV_STATUS.STA_STAMP)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        private PDFDocImage m_stamp_dimg;
        private WriteableBitmap m_stamp_bmp;
        private async void load_stamp_bmp()
        {
            Stream str1 = await Package.Current.InstalledLocation.OpenStreamForReadAsync("Assets\\imgs\\pdf_custom_stamp.png");
            WriteableBitmap bmp = new WriteableBitmap(128, 128);//bitmap size shall match resource image.
            bmp.SetSource(str1.AsRandomAccessStream());
            m_stamp_bmp = bmp;
        }
        public void PDFStampEnd()
        {
            if (m_status == PDFV_STATUS.STA_STAMP)
            {
                if (m_stamp_dimg == null)
                    m_stamp_dimg = m_doc.NewImage(m_stamp_bmp, true, true);
                long[] pages = new long[128];
                int cur;
                int end;
                int pages_cnt = 0;
                int pt_cur = 0;
                int pt_end = m_rects_cnt * 2;
                while (pt_cur < pt_end)
                {
                    RDRect rect;
                    RDPoint pt0 = m_rects[pt_cur];
                    RDPoint pt1 = m_rects[pt_cur + 1];
                    int pageno = m_content.vGetPage(pt0.x, pt0.y);
                    if (pageno >= 0)
                    {
                        long vpage = m_content.vGetPage(pageno);
                        cur = 0;
                        end = pages_cnt;
                        while (cur < end)
                        {
                            if (pages[cur] == vpage) break;
                            cur++;
                        }
                        if (cur >= end)
                        {
                            pages[cur] = vpage;
                            pages_cnt++;
                        }
                        if (pt0.x > pt1.x)
                        {
                            rect.right = pt0.x;
                            rect.left = pt1.x;
                        }
                        else
                        {
                            rect.left = pt0.x;
                            rect.right = pt1.x;
                        }
                        if (pt0.y > pt1.y)
                        {
                            rect.bottom = pt0.y;
                            rect.top = pt1.y;
                        }
                        else
                        {
                            rect.top = pt0.y;
                            rect.bottom = pt1.y;
                        }
                        PDFPage page = m_doc.GetPage(pageno);
                        page.ObjsStart();
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                        rect = mat.TransformRect(rect);
                        page.AddAnnotBitmap(m_stamp_dimg, rect);
                        m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                        page.Close();
                    }
                    pt_cur += 2;
                }
                if (m_rects_cnt != 0)
                    m_modified = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;

                cur = 0;
                end = pages_cnt;
                while (cur < end)
                {
                    m_content.vRenderPage(pages[cur]);
                    if (m_listener != null) m_listener.OnPDFPageUpdated(PDFVContent.pgGetPageNo(pages[cur]));
                    cur++;
                }
                vDraw();
                m_scroller.IsEnabled = true;
            }
        }
        private RDInk m_ink;
        private Windows.UI.Xaml.Shapes.Path m_ink_path;
        //private int m_ink_pos;
        public bool PDFInkStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_scroller.IsEnabled = false;
                m_ink = null;
                m_ink_path = null;
                m_ink_pos = 0;
                m_status = PDFV_STATUS.STA_INK;
                return true;
            }
            return false;
        }
        public void PDFInkCancel()
        {
            if (m_status == PDFV_STATUS.STA_INK)
            {
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
                m_ink = null;
                m_ink_path = null;
                //m_ink_pos = 0;
                vDraw();
            }
        }
        public void PDFInkEnd()
        {
            if (m_status == PDFV_STATUS.STA_INK)
            {
                if (m_ink != null)
                {
                    int pageno = m_content.vGetPage(m_hold_x, m_hold_y);
                    if (pageno >= 0)
                    {
                        long vpage = m_content.vGetPage(pageno);
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                        PDFPage page = m_doc.GetPage(pageno);
                        page.ObjsStart();
                        mat.TransformInk(m_ink);
                        page.AddAnnotInk(m_ink);
                        m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                        page.Close();
                        m_content.vRenderPage(vpage);
                        if (m_listener != null) m_listener.OnPDFPageUpdated(pageno);
                        m_modified = true;
                    }
                }
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
                m_ink = null;
                m_ink_path = null;
                //m_ink_pos = 0;
                vDraw();
            }
        }
        private RDPath m_polygon;
        public bool PDFPolygonStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_scroller.IsEnabled = false;
                m_polygon = null;
                m_ink_path = null;
                m_ink_pos = 0;
                m_status = PDFV_STATUS.STA_POLYGON;
                return true;
            }
            return false;
        }
        public void PDFPolygonCancel()
        {
            if (m_status == PDFV_STATUS.STA_POLYGON)
            {
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
                m_polygon = null;
                m_ink_path = null;
                //m_ink_pos = 0;
                vDraw();
            }
        }
        public void PDFPolygonEnd()
        {
            if (m_status == PDFV_STATUS.STA_POLYGON)
            {
                if (m_polygon != null)
                {
                    int pageno = m_content.vGetPage(m_hold_x, m_hold_y);
                    if (pageno >= 0)
                    {
                        long vpage = m_content.vGetPage(pageno);
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                        PDFPage page = m_doc.GetPage(pageno);
                        page.ObjsStart();
                        mat.TransformPath(m_polygon);
                        page.AddAnnotPolygon(m_polygon, sm_lineColor, sm_lineColorFill, sm_lineWidth / (float)PDFVContent.pgGetScale(vpage));
                        m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                        page.Close();
                        m_content.vRenderPage(vpage);
                        if (m_listener != null) m_listener.OnPDFPageUpdated(pageno);
                        m_modified = true;
                    }
                }
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
                m_polygon = null;
                m_ink_path = null;
                //m_ink_pos = 0;
                vDraw();
            }
        }
        public bool PDFPolylineStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_scroller.IsEnabled = false;
                m_polygon = null;
                m_ink_path = null;
                m_ink_pos = 0;
                m_status = PDFV_STATUS.STA_POLYLINE;
                return true;
            }
            return false;
        }
        public void PDFPolylineCancel()
        {
            if (m_status == PDFV_STATUS.STA_POLYLINE)
            {
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
                m_polygon = null;
                m_ink_path = null;
                //m_ink_pos = 0;
                vDraw();
            }
        }
        public void PDFPolylineEnd()
        {
            if (m_status == PDFV_STATUS.STA_POLYLINE)
            {
                if (m_polygon != null)
                {
                    int pageno = m_content.vGetPage(m_hold_x, m_hold_y);
                    if (pageno >= 0)
                    {
                        long vpage = m_content.vGetPage(pageno);
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                        PDFPage page = m_doc.GetPage(pageno);
                        page.ObjsStart();
                        mat.TransformPath(m_polygon);
                        page.AddAnnotPolyline(m_polygon, sm_lineColor, 0, 0, sm_lineColorFill, sm_lineWidth / (float)PDFVContent.pgGetScale(vpage));
                        m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                        page.Close();
                        m_content.vRenderPage(vpage);
                        if (m_listener != null) m_listener.OnPDFPageUpdated(pageno);
                        m_modified = true;
                    }
                }
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
                m_polygon = null;
                m_ink_path = null;
                //m_ink_pos = 0;
                vDraw();
            }
        }
        public bool PDFLineStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_LINE;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }
        public void PDFLineCancel()
        {
            if (m_status == PDFV_STATUS.STA_RECT)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_LINE;
                vDraw();
            }
        }
        public void PDFLineEnd()
        {
            if (m_status == PDFV_STATUS.STA_LINE)
            {
                long[] pages = new long[128];
                int cur;
                int end;
                int pages_cnt = 0;
                int pt_cur = 0;
                int pt_end = (m_rects_cnt << 1);
                while (pt_cur < pt_end)
                {
                    RDPoint pt0 = m_rects[pt_cur];
                    RDPoint pt1 = m_rects[pt_cur + 1];
                    int pageno = m_content.vGetPage(pt0.x, pt0.y);
                    if (pageno >= 0)
                    {
                        long vpage = m_content.vGetPage(pageno);
                        cur = 0;
                        end = pages_cnt;
                        while (cur < end)
                        {
                            if (pages[cur] == vpage) break;
                            cur++;
                        }
                        if (cur >= end)
                        {
                            pages[cur] = vpage;
                            pages_cnt++;
                        }
                        PDFPage page = m_doc.GetPage(pageno);
                        page.ObjsStart();
                        RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                        pt0 = mat.TransformPoint(pt0);
                        pt1 = mat.TransformPoint(pt1);
                        page.AddAnnotLine(pt0.x, pt0.y, pt1.x, pt1.y, 1, 0, sm_lineWidth / (float)PDFVContent.pgGetScale(vpage), sm_lineColor, sm_lineColorFill);
                        m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                        page.Close();
                    }
                    pt_cur += 2;
                }
                if (m_rects_cnt != 0)
                    m_modified = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;

                cur = 0;
                end = pages_cnt;
                while (cur < end)
                {
                    m_content.vRenderPage(pages[cur]);
                    if (m_listener != null) m_listener.OnPDFPageUpdated(PDFVContent.pgGetPageNo(pages[cur]));
                    cur++;
                }
                vDraw();
                m_scroller.IsEnabled = true;
            }
        }
        private struct PDFNoteRec
        {
            public long vpage;
            public int index;
        };
        private PDFNoteRec[] m_notes = new PDFNoteRec[256];
        private int m_notes_cnt;
        public bool PDFNoteStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_scroller.IsEnabled = false;
                m_status = PDFV_STATUS.STA_NOTE;
                m_notes_cnt = 0;
            }
            return true;
        }

        public void PDFNoteRemoveLast()
        {
            if (m_notes_cnt <= 0) return;
            long vpage = m_notes[m_notes_cnt - 1].vpage;
            int index = m_notes[m_notes_cnt - 1].index;
            PDFPage page = m_doc.GetPage(PDFVContent.pgGetPageNo(vpage));
            if (page != null)
            {
                page.ObjsStart();
                PDFAnnot annot = page.GetAnnot(index);
                annot.RemoveFromPage();
                page.Close();
                m_notes[m_notes_cnt - 1].vpage = 0;
                m_notes_cnt--;
                m_content.vRenderPage(vpage);
                vDraw();
            }
        }
        public void PDFNoteCancel()
        {
            if (m_status == PDFV_STATUS.STA_NOTE)
            {
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
                long[] vpages = new long[256];
                int vpages_cnt = 0;
                int index;
                for (int cur = m_notes_cnt - 1; cur >= 0; cur--)
                {
                    long vpage = m_notes[cur].vpage;
                    for (index = 0; index < vpages_cnt; index++)
                    {
                        if (vpages[index] == vpage) break;
                    }
                    if (index >= vpages_cnt)
                    {
                        vpages[vpages_cnt] = vpage;
                        vpages_cnt++;
                    }
                    PDFPage page = m_doc.GetPage(PDFVContent.pgGetPageNo(vpage));
                    if (page != null)
                    {
                        page.ObjsStart();
                        PDFAnnot annot = page.GetAnnot(m_notes[cur].index);
                        annot.RemoveFromPage();
                        page.Close();
                    }
                    m_notes[cur].vpage = 0;
                }
                m_notes_cnt = 0;
                for (index = 0; index < vpages_cnt; index++)
                    m_content.vRenderPage(vpages[index]);
                vDraw();
            }
        }
        public void PDFNoteEnd()
        {
            if (m_status == PDFV_STATUS.STA_NOTE)
            {
                m_scroller.IsEnabled = true;
                m_status = PDFV_STATUS.STA_NONE;
                if (m_notes_cnt > 0)
                    m_modified = true;
                for (int cur = m_notes_cnt - 1; cur >= 0; cur--)
                    m_notes[cur].vpage = 0;
                m_notes_cnt = 0;
                vDraw();
            }
        }
        public bool PDFEditTextStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_TEXT_EDIT;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }

        public void PDFEditTextCancel()
        {
            if (m_status == PDFV_STATUS.STA_TEXT_EDIT)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }

        public void PDFEditTextEnd()
        {
            if (m_status != PDFV_STATUS.STA_TEXT_EDIT) return;
            long[] pages = new long[128];
            int cur;
            int end;
            int pages_cnt = 0;
            int pt_cur = 0;
            int pt_end = m_rects_cnt * 2;
            //double tmp = 1 / m_scroller.ZoomFactor;
            while (pt_cur < pt_end)
            {
                RDRect rect;
                RDPoint pt0 = m_rects[pt_cur];
                RDPoint pt1 = m_rects[pt_cur + 1];
                int pageno = m_content.vGetPage(pt0.x, pt0.y);
                if (pageno >= 0)
                {
                    long vpage = m_content.vGetPage(pageno);
                    cur = 0;
                    end = pages_cnt;
                    while (cur < end)
                    {
                        if (pages[cur] == vpage) break;
                        cur++;
                    }
                    if (cur >= end)
                    {
                        pages[cur] = vpage;
                        pages_cnt++;
                    }
                    if (pt0.x > pt1.x)
                    {
                        rect.right = (float)(pt0.x);
                        rect.left = (float)(pt1.x);
                    }
                    else
                    {
                        rect.left = (float)(pt0.x);
                        rect.right = (float)(pt1.x);
                    }
                    if (pt0.y > pt1.y)
                    {
                        rect.bottom = (float)(pt0.y);
                        rect.top = (float)(pt1.y);
                    }
                    else
                    {
                        rect.top = (float)(pt0.y);
                        rect.bottom = (float)(pt1.y);
                    }
                    PDFPage page = m_doc.GetPage(pageno);
                    page.ObjsStart();
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                    rect = mat.TransformRect(rect);
                    if (rect.right - rect.left < 80) rect.right = rect.left + 80;
                    if (rect.bottom - rect.top < 16) rect.top = rect.bottom - 16;
                    page.AddAnnotEditbox(rect, 0xFF000000, 2, 0x00FFFFFF, 12.0f, sm_textColor);//transparent background
                    //page.AddAnnotEditbox(rect, 0xFF000000, 2, 0xFFFFFFFF, 12.0f, sm_textColor);//white background
                    m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                    page.Close();
                }
                pt_cur += 2;
            }
            if (m_rects_cnt != 0)
                m_modified = true;
            m_rects_cnt = 0;
            m_status = PDFV_STATUS.STA_NONE;

            cur = 0;
            end = pages_cnt;
            while (cur < end)
            {
                m_content.vRenderPage(pages[cur]);
                cur++;
            }
            vDraw();
            m_scroller.IsEnabled = true;
        }
        public bool PDFFieldEditTextStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_FIELD_TEXT_EDIT;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }

        public void PDFFieldEditTextCancel()
        {
            if (m_status == PDFV_STATUS.STA_FIELD_TEXT_EDIT)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }

        public void PDFFieldEditTextEnd()
        {
            if (m_status != PDFV_STATUS.STA_FIELD_TEXT_EDIT) return;
            long[] pages = new long[128];
            int cur;
            int end;
            int pages_cnt = 0;
            int pt_cur = 0;
            int pt_end = m_rects_cnt * 2;
            //double tmp = 1 / m_scroller.ZoomFactor;
            while (pt_cur < pt_end)
            {
                RDRect rect;
                RDPoint pt0 = m_rects[pt_cur];
                RDPoint pt1 = m_rects[pt_cur + 1];
                int pageno = m_content.vGetPage(pt0.x, pt0.y);
                if (pageno >= 0)
                {
                    long vpage = m_content.vGetPage(pageno);
                    cur = 0;
                    end = pages_cnt;
                    while (cur < end)
                    {
                        if (pages[cur] == vpage) break;
                        cur++;
                    }
                    if (cur >= end)
                    {
                        pages[cur] = vpage;
                        pages_cnt++;
                    }
                    if (pt0.x > pt1.x)
                    {
                        rect.right = (float)(pt0.x);
                        rect.left = (float)(pt1.x);
                    }
                    else
                    {
                        rect.left = (float)(pt0.x);
                        rect.right = (float)(pt1.x);
                    }
                    if (pt0.y > pt1.y)
                    {
                        rect.bottom = (float)(pt0.y);
                        rect.top = (float)(pt1.y);
                    }
                    else
                    {
                        rect.top = (float)(pt0.y);
                        rect.bottom = (float)(pt1.y);
                    }
                    PDFPage page = m_doc.GetPage(pageno);
                    page.ObjsStart();
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                    rect = mat.TransformRect(rect);
                    if (rect.right - rect.left < 80) rect.right = rect.left + 80;
                    if (rect.bottom - rect.top < 16) rect.top = rect.bottom - 16;
                    int isign = 1;
                    String sname = String.Format("rdedit{0:D3}", isign++);
                    while (!page.AddFieldEditbox(rect, sname, false, false))//transparent background
                        sname = String.Format("rdedit{0:D3}", isign++);
                    //page.AddAnnotEditbox(rect, 0xFF000000, 2, 0xFFFFFFFF, 12.0f, sm_textColor);//white background
                    m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                    page.Close();
                }
                pt_cur += 2;
            }
            if (m_rects_cnt != 0)
                m_modified = true;
            m_rects_cnt = 0;
            m_status = PDFV_STATUS.STA_NONE;

            cur = 0;
            end = pages_cnt;
            while (cur < end)
            {
                m_content.vRenderPage(pages[cur]);
                cur++;
            }
            vDraw();
            m_scroller.IsEnabled = true;
        }
        public bool PDFFieldCheckStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_FIELD_CHECK;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }

        public void PDFFieldCheckCancel()
        {
            if (m_status == PDFV_STATUS.STA_FIELD_CHECK)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public void PDFFieldCheckEnd()
        {
            if (m_status != PDFV_STATUS.STA_FIELD_CHECK) return;
            long[] pages = new long[128];
            int cur;
            int end;
            int pages_cnt = 0;
            int pt_cur = 0;
            int pt_end = m_rects_cnt * 2;
            //double tmp = 1 / m_scroller.ZoomFactor;
            while (pt_cur < pt_end)
            {
                RDRect rect;
                RDPoint pt0 = m_rects[pt_cur];
                RDPoint pt1 = m_rects[pt_cur + 1];
                int pageno = m_content.vGetPage(pt0.x, pt0.y);
                if (pageno >= 0)
                {
                    long vpage = m_content.vGetPage(pageno);
                    cur = 0;
                    end = pages_cnt;
                    while (cur < end)
                    {
                        if (pages[cur] == vpage) break;
                        cur++;
                    }
                    if (cur >= end)
                    {
                        pages[cur] = vpage;
                        pages_cnt++;
                    }
                    if (pt0.x > pt1.x)
                    {
                        rect.right = (float)(pt0.x);
                        rect.left = (float)(pt1.x);
                    }
                    else
                    {
                        rect.left = (float)(pt0.x);
                        rect.right = (float)(pt1.x);
                    }
                    if (pt0.y > pt1.y)
                    {
                        rect.bottom = (float)(pt0.y);
                        rect.top = (float)(pt1.y);
                    }
                    else
                    {
                        rect.top = (float)(pt0.y);
                        rect.bottom = (float)(pt1.y);
                    }
                    PDFPage page = m_doc.GetPage(pageno);
                    page.ObjsStart();
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                    rect = mat.TransformRect(rect);
                    if (rect.right - rect.left < 20) rect.right = rect.left + 20;
                    if (rect.bottom - rect.top < 20) rect.top = rect.bottom - 20;
                    int isign = 1;
                    String sname = String.Format("rdchk{0:D3}", isign++);
                    while (!page.AddFieldCheck(rect, sname, sname, null, null))//transparent background
                        sname = String.Format("rdchk{0:D3}", isign++);
                    //page.AddAnnotEditbox(rect, 0xFF000000, 2, 0xFFFFFFFF, 12.0f, sm_textColor);//white background
                    m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                    page.Close();
                }
                pt_cur += 2;
            }
            if (m_rects_cnt != 0)
                m_modified = true;
            m_rects_cnt = 0;
            m_status = PDFV_STATUS.STA_NONE;

            cur = 0;
            end = pages_cnt;
            while (cur < end)
            {
                m_content.vRenderPage(pages[cur]);
                cur++;
            }
            vDraw();
            m_scroller.IsEnabled = true;
        }
        public bool PDFFieldSignStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_FIELD_SIGNATURE;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }

        public void PDFFieldSignCancel()
        {
            if (m_status == PDFV_STATUS.STA_FIELD_SIGNATURE)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public void PDFFieldSignEnd()
        {
            if (m_status != PDFV_STATUS.STA_FIELD_SIGNATURE) return;
            long[] pages = new long[128];
            int cur;
            int end;
            int pages_cnt = 0;
            int pt_cur = 0;
            int pt_end = m_rects_cnt * 2;
            //double tmp = 1 / m_scroller.ZoomFactor;
            while (pt_cur < pt_end)
            {
                RDRect rect;
                RDPoint pt0 = m_rects[pt_cur];
                RDPoint pt1 = m_rects[pt_cur + 1];
                int pageno = m_content.vGetPage(pt0.x, pt0.y);
                if (pageno >= 0)
                {
                    long vpage = m_content.vGetPage(pageno);
                    cur = 0;
                    end = pages_cnt;
                    while (cur < end)
                    {
                        if (pages[cur] == vpage) break;
                        cur++;
                    }
                    if (cur >= end)
                    {
                        pages[cur] = vpage;
                        pages_cnt++;
                    }
                    if (pt0.x > pt1.x)
                    {
                        rect.right = (float)(pt0.x);
                        rect.left = (float)(pt1.x);
                    }
                    else
                    {
                        rect.left = (float)(pt0.x);
                        rect.right = (float)(pt1.x);
                    }
                    if (pt0.y > pt1.y)
                    {
                        rect.bottom = (float)(pt0.y);
                        rect.top = (float)(pt1.y);
                    }
                    else
                    {
                        rect.top = (float)(pt0.y);
                        rect.bottom = (float)(pt1.y);
                    }
                    PDFPage page = m_doc.GetPage(pageno);
                    page.ObjsStart();
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                    rect = mat.TransformRect(rect);
                    if (rect.right - rect.left < 100) rect.right = rect.left + 100;
                    if (rect.bottom - rect.top < 60) rect.top = rect.bottom - 60;
                    int isign = 1;
                    String sname = String.Format("rdsign{0:D3}", isign++);
                    while (!page.AddFieldSign(rect, sname))//transparent background
                        sname = String.Format("rdsign{0:D3}", isign++);
                    //page.AddAnnotEditbox(rect, 0xFF000000, 2, 0xFFFFFFFF, 12.0f, sm_textColor);//white background
                    m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                    page.Close();
                }
                pt_cur += 2;
            }
            if (m_rects_cnt != 0)
                m_modified = true;
            m_rects_cnt = 0;
            m_status = PDFV_STATUS.STA_NONE;

            cur = 0;
            end = pages_cnt;
            while (cur < end)
            {
                m_content.vRenderPage(pages[cur]);
                cur++;
            }
            vDraw();
            m_scroller.IsEnabled = true;
        }

        private int m_radio_nidx = 0;
        private int m_radio_val = 0;
        public bool PDFFieldRadioStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_FIELD_RADIO;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                return true;
            }
            return false;
        }

        public void PDFFieldRadioCancel()
        {
            if (m_status == PDFV_STATUS.STA_FIELD_RADIO)
            {
                m_scroller.IsEnabled = true;
                m_rects_cnt = 0;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public void PDFFieldRadioEnd()
        {
            if (m_status != PDFV_STATUS.STA_FIELD_RADIO) return;
            long[] pages = new long[128];
            int cur;
            int end;
            int pages_cnt = 0;
            int pt_cur = 0;
            int pt_end = m_rects_cnt * 2;
            //double tmp = 1 / m_scroller.ZoomFactor;
            String sname = String.Format("rdrad{0:D3}", m_radio_nidx++);//increase group number.
            while (pt_cur < pt_end)
            {
                RDRect rect;
                RDPoint pt0 = m_rects[pt_cur];
                RDPoint pt1 = m_rects[pt_cur + 1];
                int pageno = m_content.vGetPage(pt0.x, pt0.y);
                if (pageno >= 0)
                {
                    long vpage = m_content.vGetPage(pageno);
                    cur = 0;
                    end = pages_cnt;
                    while (cur < end)
                    {
                        if (pages[cur] == vpage) break;
                        cur++;
                    }
                    if (cur >= end)
                    {
                        pages[cur] = vpage;
                        pages_cnt++;
                    }
                    if (pt0.x > pt1.x)
                    {
                        rect.right = (float)(pt0.x);
                        rect.left = (float)(pt1.x);
                    }
                    else
                    {
                        rect.left = (float)(pt0.x);
                        rect.right = (float)(pt1.x);
                    }
                    if (pt0.y > pt1.y)
                    {
                        rect.bottom = (float)(pt0.y);
                        rect.top = (float)(pt1.y);
                    }
                    else
                    {
                        rect.top = (float)(pt0.y);
                        rect.bottom = (float)(pt1.y);
                    }
                    PDFPage page = m_doc.GetPage(pageno);
                    page.ObjsStart();
                    RDMatrix mat = PDFVContent.pgCreateInvertMatrix(vpage, m_scroller.HorizontalOffset, m_scroller.VerticalOffset);
                    rect = mat.TransformRect(rect);
                    if (rect.right - rect.left < 20) rect.right = rect.left + 20;
                    if (rect.bottom - rect.top < 20) rect.top = rect.bottom - 20;
                    String sval = String.Format("rad{0:D3}", m_radio_val++);
                    page.AddFieldRadio(rect, sname, sval, null, null);
                    m_opstack.push(PDFOPStack.new_add(pageno, page, page.AnnotCount - 1));
                    page.Close();
                }
                pt_cur += 2;
            }
            if (m_rects_cnt != 0)
                m_modified = true;
            m_rects_cnt = 0;
            m_status = PDFV_STATUS.STA_NONE;

            cur = 0;
            end = pages_cnt;
            while (cur < end)
            {
                m_content.vRenderPage(pages[cur]);
                cur++;
            }
            vDraw();
            m_scroller.IsEnabled = true;
        }
        private PDFEditNode m_edit_node = null;
        private DispatcherTimer m_timer;
        private int m_edit_caret_sta = 0;
        private RDRect m_edit_caret_rect;
        private long m_edit_pos0 = -1;
        private long m_edit_pos1 = -1;
        private void timer_tick(object sender, object e)
        {
            if (m_canvas == null) return;
            m_edit_caret_sta ^= 2;
            //draw out
            m_canvas.clear();
            vDrawPageEdit();
        }
        private void caret_show(bool show)
        {
            if ((m_edit_caret_sta & 1) != 0 && !show)
            {
                m_edit_caret_sta &= (~3);
                m_timer.Stop();
                m_timer.Tick -= timer_tick;
                m_timer = null;
                m_edit_caret_rect.left = 0;
                m_edit_caret_rect.top = 0;
                m_edit_caret_rect.right = 0;
                m_edit_caret_rect.bottom = 0;
            }
            if ((m_edit_caret_sta & 1) == 0 && show)
            {
                m_edit_caret_sta |= 3;
                m_timer = new DispatcherTimer();
                m_timer.Interval = new TimeSpan(0, 0, 0, 0, 500);
                m_timer.Tick += timer_tick;
                m_timer.Start();
            }
        }
        public bool PDFPageEditStart()
        {
            if (m_doc == null || !m_doc.CanSave || m_content == null) return false;
            if (m_status == PDFV_STATUS.STA_NONE)
            {
                m_status = PDFV_STATUS.STA_EDIT_CONTENT;
                m_rects_cnt = 0;
                m_scroller.IsEnabled = false;
                caret_show(true);
                return true;
            }
            return false;
        }
        public void PDFPageEditCancel()
        {
            if (m_status == PDFV_STATUS.STA_EDIT_CONTENT)
            {
                m_scroller.IsEnabled = true;

                int pcur;
                int pcnt = m_doc.PageCount;
                for (pcur = 0; pcur < pcnt; pcur++)
                {
                    PDFPage page = m_doc.GetPage(pcur);
                    //bool ret = page.UpdateWithPGEditor();
                    bool ret = page.CancelWithPGEditor();
                    page.Close();
                    if (ret)
                    {
                        long vp = m_content.vGetPage(pcur);//reload all pages
                        m_content.vRenderPage(vp);
                    }
                }
                m_modified = true;
                caret_show(false);
                if (m_annot_page != null)
                {
                    m_annot_page.Close();
                    m_annot_page = null;
                }
                m_edit_pos0 = -1;
                m_edit_pos1 = -1;
                m_edit_node = null;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public void PDFPageEditEnd()
        {
            if (m_status == PDFV_STATUS.STA_EDIT_CONTENT)
            {
                m_scroller.IsEnabled = true;

                int pcur;
                int pcnt = m_doc.PageCount;
                for (pcur = 0; pcur < pcnt; pcur++)
                {
                    PDFPage page = m_doc.GetPage(pcur);
                    bool ret = page.UpdateWithPGEditor();//rewrite page content with editor.
                    page.Close();
                    if (ret)
                    {
                        long vp = m_content.vGetPage(pcur);//reload all pages
                        m_content.vRenderPage(vp);
                    }
                }
                m_modified = true;
                caret_show(false);
                if (m_annot_page != null)
                {
                    m_annot_page.Close();
                    m_annot_page = null;
                }
                m_edit_pos0 = -1;
                m_edit_pos1 = -1;
                m_edit_node = null;
                m_status = PDFV_STATUS.STA_NONE;
                vDraw();
            }
        }
        public void PDFFindStart(String pat, bool match_case, bool whole_word)
        {
            m_content.vFindStart(pat, match_case, whole_word);
        }
        public void PDFFind(int dir)
        {
            m_content.vFind(dir);
        }
        public void PDFFindEnd()
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
                        m_status = PDFV_STATUS.STA_NONE;
                        if (m_listener != null)
                            m_listener.OnPDFLongPressed((float)point.X, (float)point.Y);
                    }
                }
            }
            m_scroller.ChangeView(m_shold_x + m_hold_x - point.X, m_shold_y + m_hold_y - point.Y, m_scroller.ZoomFactor, true);
        }
        private bool OnSelTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_SELECT) return false;
            m_hold_x = point.X;
            m_hold_y = point.Y;
            m_annot_pos = m_content.vGetPos(m_hold_x, m_hold_y);
            m_sel = new RDVSel(m_doc, m_annot_pos.pageno, to_contx(m_scroller.HorizontalOffset), to_conty(m_scroller.VerticalOffset));
            return true;
        }
        private bool OnSelTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_SELECT) return false;
            long vpage = m_content.vGetPage(m_annot_pos.pageno);
            double pdfx = PDFVContent.pgGetPDFX(vpage, point.X + m_scroller.HorizontalOffset);
            double pdfy = PDFVContent.pgGetPDFY(vpage, point.Y + m_scroller.VerticalOffset);
            m_sel.SetSel((float)m_annot_pos.x, (float)m_annot_pos.y, (float)pdfx, (float)pdfy);
            vDraw();
            return true;
        }
        private bool OnSelTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_SELECT) return false;
            long vpage = m_content.vGetPage(m_annot_pos.pageno);
            double pdfx = PDFVContent.pgGetPDFX(vpage, point.X + m_scroller.HorizontalOffset);
            double pdfy = PDFVContent.pgGetPDFY(vpage, point.Y + m_scroller.VerticalOffset);
            m_sel.SetSel((float)m_annot_pos.x, (float)m_annot_pos.y, (float)pdfx, (float)pdfy);
            vDraw();
            if (m_listener != null)
                m_listener.OnPDFSelected();
            return true;
        }
        private RDPoint[] m_rects = new RDPoint[256];
        private int m_rects_cnt;
        private bool OnAnnotTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_ANNOT || m_annot.Locked) return false;
            m_hold_x = point.X;
            m_hold_y = point.Y;
            m_shold_x = m_hold_x;
            m_shold_y = m_hold_y;
            m_aop.OnTouchBeg(point);
            return true;
        }
        private bool OnAnnotTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_ANNOT || m_annot.Locked) return false;
            if (m_doc != null && m_doc.CanSave)
            {
                m_shold_x = (float)point.X;
                m_shold_y = (float)point.Y;
                m_aop.OnTouchMove(point);
            }
            vDraw();
            return true;
        }
        private bool OnAnnotTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_ANNOT || m_annot.Locked) return false;

            if (m_doc != null && m_doc.CanSave)
                m_aop.OnTouchEnd(point);
            return true;
        }
        private bool OnNoteTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_NOTE) return false;
            return true;
        }
        private bool OnNoteTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_NOTE) return false;
            return true;
        }
        private bool OnNoteTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_NOTE) return false;
            PDFPos pos = m_content.vGetPos(point.X, point.Y);
            long vpage = m_content.vGetPage(pos.pageno);
            if (vpage != 0)
            {
                PDFPage page = m_doc.GetPage(pos.pageno);
                if (page != null)
                {
                    page.ObjsStart();
                    if (page.AddAnnotTextNote((float)pos.x, (float)pos.y))
                    {
                        m_notes[m_notes_cnt].vpage = vpage;
                        m_notes[m_notes_cnt].index = page.AnnotCount - 1;
                        m_opstack.push(PDFOPStack.new_add(pos.pageno, page, page.AnnotCount - 1));
                        m_content.vRenderPage(vpage);
                        vDraw();
                        if (m_listener != null)
                        {
                            PDFPage page1 = m_doc.GetPage(pos.pageno);
                            page1.ObjsStart();
                            PDFAnnot annot = page1.GetAnnot(m_notes[m_notes_cnt].index);
                            m_listener.OnPDFAnnotPopup(annot, annot.PopupSubject, annot.PopupText);
                        }
                        m_notes_cnt++;
                    }
                    page.Close();
                }
            }
            return true;
        }
        private bool OnRectTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_RECT) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnRectTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_RECT) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        /// <summary>
        /// touch end on rect status.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool OnRectTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_RECT) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            //vRectEnd();
            return true;
        }
        private bool OnStampTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_STAMP) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnStampTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_STAMP) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        /// <summary>
        /// touch end on rect status.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool OnStampTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_STAMP) return false;
            int idx = (m_rects_cnt << 1) - 1;
            m_rects[idx].x = (float)point.X;
            m_rects[idx].y = (float)point.Y;
            if (m_rects[idx].x == m_rects[idx - 1].x &&
                m_rects[idx].y == m_rects[idx - 1].y)
            {
                m_rects[idx].x += 1;
                m_rects[idx].y += 1;
            }
            vDraw();
            //vRectEnd();
            return true;
        }
        private bool OnInkTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_INK) return false;
            if (m_ink == null)
            {
                //(宽度，颜色)
                m_hold_x = (float)point.X;
                m_hold_y = (float)point.Y;
                m_ink = new RDInk(inkWidth, sm_inkColor);
                m_ink_path = new Windows.UI.Xaml.Shapes.Path();
                m_ink_path.Data = new PathGeometry();
                //m_ink_pos = 0;
                Windows.UI.Color clr;
                clr.A = (byte)(sm_inkColor >> 24);
                clr.R = (byte)(sm_inkColor >> 16);
                clr.G = (byte)(sm_inkColor >> 8);
                clr.B = (byte)(sm_inkColor);
                m_ink_path.Stroke = new SolidColorBrush(clr);
                m_ink_path.StrokeThickness = inkWidth;
                m_ink_path.StrokeStartLineCap = PenLineCap.Round;
                m_ink_path.StrokeLineJoin = PenLineJoin.Round;
            }
            m_ink.Down((float)point.X, (float)point.Y);
            PathFigure inkf = new PathFigure();
            ((PathGeometry)m_ink_path.Data).Figures.Add(inkf);
            return true;
        }
        private bool OnInkTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_INK) return false;
            m_ink.Move((float)point.X, (float)point.Y);
            vDraw();
            return true;
        }
        private bool OnInkTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_INK) return false;
            m_ink.Up((float)point.X, (float)point.Y);
            vDraw();
            return true;
        }
        private bool OnPolygonTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_POLYGON) return false;
            return true;
        }
        private bool OnPolygonTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_POLYGON) return false;
            return true;
        }
        private bool OnPolygonTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_POLYGON) return false;
            if (m_polygon == null)
            {
                //(宽度，颜色)
                m_hold_x = (float)point.X;
                m_hold_y = (float)point.Y;
                m_polygon = new RDPath();
                m_ink_path = new Windows.UI.Xaml.Shapes.Path();
                m_ink_path.Data = new PathGeometry();
                //m_ink_pos = 0;
                Windows.UI.Color clr;
                clr.A = (byte)(sm_lineColor >> 24);
                clr.R = (byte)(sm_lineColor >> 16);
                clr.G = (byte)(sm_lineColor >> 8);
                clr.B = (byte)(sm_lineColor);
                m_ink_path.Stroke = new SolidColorBrush(clr);
                m_ink_path.StrokeThickness = inkWidth;
                m_ink_path.StrokeStartLineCap = PenLineCap.Round;
                m_ink_path.StrokeLineJoin = PenLineJoin.Round;

                clr.A = (byte)(sm_lineColorFill >> 24);
                clr.R = (byte)(sm_lineColorFill >> 16);
                clr.G = (byte)(sm_lineColorFill >> 8);
                clr.B = (byte)(sm_lineColorFill);
                m_ink_path.Fill = new SolidColorBrush(clr);
                PathFigure inkf = new PathFigure();
                ((PathGeometry)m_ink_path.Data).Figures.Add(inkf);
                m_polygon.MoveTo((float)point.X, (float)point.Y);
                //inkf.IsClosed = true;
            }
            else m_polygon.LineTo((float)point.X, (float)point.Y);
            vDraw();
            return true;
        }
        private bool OnPolylineTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_POLYLINE) return false;
            return true;
        }
        private bool OnPolylineTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_POLYLINE) return false;
            return true;
        }
        private bool OnPolylineTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_POLYLINE) return false;
            if (m_polygon == null)
            {
                //(宽度，颜色)
                m_hold_x = (float)point.X;
                m_hold_y = (float)point.Y;
                m_polygon = new RDPath();
                m_ink_path = new Windows.UI.Xaml.Shapes.Path();
                m_ink_path.Data = new PathGeometry();
                //m_ink_pos = 0;
                Windows.UI.Color clr;
                clr.A = (byte)(sm_lineColor >> 24);
                clr.R = (byte)(sm_lineColor >> 16);
                clr.G = (byte)(sm_lineColor >> 8);
                clr.B = (byte)(sm_lineColor);
                m_ink_path.Stroke = new SolidColorBrush(clr);
                m_ink_path.StrokeThickness = inkWidth;
                m_ink_path.StrokeStartLineCap = PenLineCap.Round;
                m_ink_path.StrokeLineJoin = PenLineJoin.Round;
                PathFigure inkf = new PathFigure();
                ((PathGeometry)m_ink_path.Data).Figures.Add(inkf);
                m_polygon.MoveTo((float)point.X, (float)point.Y);
            }
            else m_polygon.LineTo((float)point.X, (float)point.Y);
            vDraw();
            return true;
        }
        private bool OnLineTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_LINE) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnLineTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_LINE) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        private bool OnLineTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_LINE) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        private bool OnEditTextBoxTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_TEXT_EDIT) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnEditTextBoxTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_TEXT_EDIT) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        private bool OnEditTextBoxTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_TEXT_EDIT) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;

            point.X = to_contx(point.X);
            point.Y = to_conty(point.Y);
            m_annot_pos = m_content.vGetPos(point.X, point.Y);
            long vpage = m_content.vGetPage(m_annot_pos.pageno);
            m_annot_page = m_doc.GetPage(PDFVContent.pgGetPageNo(vpage));
            PDFEditTextEnd();

            m_annot_page.ObjsStart();

            m_annot = m_annot_page.GetAnnot(m_annot_page.AnnotCount - 1);
            m_scroller.IsEnabled = false;
            m_status = PDFV_STATUS.STA_ANNOT;
            m_aop = m_aop_normal;
            m_annot_rect = m_annot.Rect;
            m_annot_rect.left = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, m_annot_rect.left));
            m_annot_rect.right = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, m_annot_rect.right));
            float tmp = m_annot_rect.top;
            m_annot_rect.top = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, m_annot_rect.bottom));
            m_annot_rect.bottom = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, tmp));

            tmp = (float)m_scroller.HorizontalOffset;
            m_annot_rect.left -= tmp;
            m_annot_rect.right -= tmp;
            tmp = (float)m_scroller.VerticalOffset;
            m_annot_rect.top -= tmp;
            m_annot_rect.bottom -= tmp;
            m_shold_x = m_hold_x;
            m_shold_y = m_hold_y;
            vDraw();
            if (m_listener != null)
            {
                m_listener.OnPDFPageTapped(PDFVContent.pgGetPageNo(vpage));
                RDRect rect;
                rect.left = (float)to_canvasx(m_annot_rect.left);
                rect.top = (float)to_canvasy(m_annot_rect.top);
                rect.right = (float)to_canvasx(m_annot_rect.right);
                rect.bottom = (float)to_canvasy(m_annot_rect.bottom);
                m_listener.OnPDFAnnotClicked(m_annot_page, m_annot_pos.pageno, m_annot, rect);
            }
            return true;
        }
        private bool OnEllipseTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_ELLIPSE) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnEllipseTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_ELLIPSE) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        private bool OnEllipseTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_ELLIPSE) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }

        private bool OnFieldEditTextBoxTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_TEXT_EDIT) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnFieldEditTextBoxTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_TEXT_EDIT) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        private bool OnFieldEditTextBoxTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_TEXT_EDIT) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;

            point.X = to_contx(point.X);
            point.Y = to_conty(point.Y);
            m_annot_pos = m_content.vGetPos(point.X, point.Y);
            long vpage = m_content.vGetPage(m_annot_pos.pageno);
            m_annot_page = m_doc.GetPage(PDFVContent.pgGetPageNo(vpage));
            PDFFieldEditTextEnd();

            m_annot_page.ObjsStart();

            m_annot = m_annot_page.GetAnnot(m_annot_page.AnnotCount - 1);
            m_scroller.IsEnabled = false;
            m_status = PDFV_STATUS.STA_ANNOT;
            m_annot_rect = m_annot.Rect;
            m_annot_rect.left = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, m_annot_rect.left));
            m_annot_rect.right = (float)(PDFVContent.pgGetLeft(vpage) + PDFVContent.pgToDIBX(vpage, m_annot_rect.right));
            float tmp = m_annot_rect.top;
            m_annot_rect.top = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, m_annot_rect.bottom));
            m_annot_rect.bottom = (float)(PDFVContent.pgGetTop(vpage) + PDFVContent.pgToDIBY(vpage, tmp));

            tmp = (float)m_scroller.HorizontalOffset;
            m_annot_rect.left -= tmp;
            m_annot_rect.right -= tmp;
            tmp = (float)m_scroller.VerticalOffset;
            m_annot_rect.top -= tmp;
            m_annot_rect.bottom -= tmp;
            m_shold_x = m_hold_x;
            m_shold_y = m_hold_y;
            vDraw();
            if (m_listener != null)
            {
                m_listener.OnPDFPageTapped(PDFVContent.pgGetPageNo(vpage));
                RDRect rect;
                rect.left = (float)to_canvasx(m_annot_rect.left);
                rect.top = (float)to_canvasy(m_annot_rect.top);
                rect.right = (float)to_canvasx(m_annot_rect.right);
                rect.bottom = (float)to_canvasy(m_annot_rect.bottom);
                m_listener.OnPDFAnnotClicked(m_annot_page, m_annot_pos.pageno, m_annot, rect);
            }
            return true;
        }
        private bool OnFieldCheckTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_CHECK) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnFieldCheckTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_CHECK) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        /// <summary>
        /// touch end on rect status.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool OnFieldCheckTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_CHECK) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            //vRectEnd();
            return true;
        }
        private bool OnFieldSignTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_SIGNATURE) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnFieldSignTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_SIGNATURE) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        /// <summary>
        /// touch end on rect status.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool OnFieldSignTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_SIGNATURE) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            //vRectEnd();
            return true;
        }
        private bool OnFieldRadioTouchBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_RADIO) return false;
            if (m_rects_cnt >= 256) return true;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;
            m_rects[m_rects_cnt << 1].x = (float)m_hold_x;
            m_rects[m_rects_cnt << 1].y = (float)m_hold_y;
            m_rects[(m_rects_cnt << 1) + 1].x = (float)m_hold_x;
            m_rects[(m_rects_cnt << 1) + 1].y = (float)m_hold_y;
            m_rects_cnt++;
            return true;
        }
        private bool OnFieldRadioTouchMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_RADIO) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            return true;
        }
        /// <summary>
        /// touch end on rect status.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool OnFieldRadioTouchEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_FIELD_RADIO) return false;
            m_rects[(m_rects_cnt << 1) - 1].x = (float)point.X;
            m_rects[(m_rects_cnt << 1) - 1].y = (float)point.Y;
            vDraw();
            //vRectEnd();
            return true;
        }
        private bool OnPageEditBegin(Point point)
        {
            if (m_status != PDFV_STATUS.STA_EDIT_CONTENT) return false;
            m_hold_x = (float)point.X;
            m_hold_y = (float)point.Y;


            m_annot_pos = m_content.vGetPos((int)m_hold_x, (int)m_hold_y);
            long vpage = m_content.vGetPage(m_annot_pos.pageno);
            m_annot_vpage = vpage;
            m_annot_page = m_doc.GetPage(m_annot_pos.pageno);
            m_edit_node = m_annot_page.GetPGEditorNode((float)m_annot_pos.x, (float)m_annot_pos.y);
            m_edit_caret_rect.left = 0;
            m_edit_caret_rect.top = 0;
            m_edit_caret_rect.right = 0;
            m_edit_caret_rect.bottom = 0;
            m_edit_pos0 = -1;
            m_edit_pos1 = -1;
            if (m_edit_node != null)
            {
                m_annot_rect = m_edit_node.Rect;
                float tmp = m_annot_rect.top;
                m_annot_rect.left = (float)(PDFVContent.pgGetVX(vpage, m_annot_rect.left) - m_content.vGetX());
                m_annot_rect.top = (float)(PDFVContent.pgGetVY(vpage, m_annot_rect.bottom) - m_content.vGetY());
                m_annot_rect.right = (float)(PDFVContent.pgGetVX(vpage, m_annot_rect.right) - m_content.vGetX());
                m_annot_rect.bottom = (float)(PDFVContent.pgGetVY(vpage, tmp) - m_content.vGetY());
                m_annot_rect0 = m_annot_rect;
                m_status = PDFV_STATUS.STA_EDIT_CONTENT;
                bool on_edge = m_hold_x < m_annot_rect.left + 8 || m_hold_x > m_annot_rect.right - 8 ||
                        m_hold_y < m_annot_rect.top + 8 || m_hold_y > m_annot_rect.bottom - 8;
                if (m_edit_node.Type == 1 && !on_edge)//text block to display caret
                {
                    m_edit_pos0 = m_edit_node.GetCharPos((float)m_annot_pos.x, (float)m_annot_pos.y);
                    m_edit_pos1 = m_edit_pos0;
                    RDRect crect = m_edit_node.GetCharRect(m_edit_pos0);
                    if (PDFEditNode.caret_is_vert(m_edit_pos0))//vertical writing mode.
                    {
                        crect.left = (float)(PDFVContent.pgGetVX(vpage, crect.left) - m_content.vGetX());
                        crect.right = (float)(PDFVContent.pgGetVX(vpage, crect.right) - m_content.vGetX());
                        tmp = (crect.right - crect.left) * 0.1f;
                        if (PDFEditNode.caret_is_end(m_edit_pos0))
                        {
                            crect.top = (float)(PDFVContent.pgGetVY(vpage, crect.bottom) - m_content.vGetY() - tmp * 0.5f);
                            crect.bottom = crect.top + tmp;
                        }
                        else
                        {
                            crect.top = (float)(PDFVContent.pgGetVY(vpage, crect.top) - m_content.vGetY() - tmp * 0.5f);
                            crect.bottom = crect.top + tmp;
                        }
                    }
                    else//horizon writing mode
                    {
                        tmp = crect.top;
                        crect.top = (float)(PDFVContent.pgGetVY(vpage, crect.bottom) - m_content.vGetY());
                        crect.bottom = (float)(PDFVContent.pgGetVY(vpage, tmp) - m_content.vGetY());
                        tmp = (crect.bottom - crect.top) * 0.1f;
                        if (PDFEditNode.caret_is_end(m_edit_pos0))
                        {
                            crect.left = (float)(PDFVContent.pgGetVX(vpage, crect.right) - m_content.vGetX() - tmp * 0.5f);
                            crect.right = crect.left + tmp;
                        }
                        else
                        {
                            crect.left = (float)(PDFVContent.pgGetVX(vpage, crect.left) - m_content.vGetX() - tmp * 0.5f);
                            crect.right = crect.left + tmp;
                        }
                    }
                    m_edit_caret_rect = crect;
                    m_edit_caret_sta |= 2;
                }
                vDraw();
            }
            return true;
        }
        private void onEditCaret()
        {
            RDRect crect = m_edit_node.GetCharRect(m_edit_pos1);
            long vpage = m_annot_vpage;
            float tmp;
            if (PDFEditNode.caret_is_vert(m_edit_pos1))//vertical writing mode.
            {
                crect.left = (float)(PDFVContent.pgGetVX(vpage, crect.left) - m_content.vGetX());
                crect.right = (float)(PDFVContent.pgGetVX(vpage, crect.right) - m_content.vGetX());
                tmp = (crect.right - crect.left) * 0.1f;
                if (PDFEditNode.caret_is_end(m_edit_pos1))
                {
                    crect.top = (float)(PDFVContent.pgGetVY(vpage, crect.bottom) - m_content.vGetY() - tmp * 0.5f);
                    crect.bottom = crect.top + tmp;
                }
                else
                {
                    crect.top = (float)(PDFVContent.pgGetVY(vpage, crect.top) - m_content.vGetY() - tmp * 0.5f);
                    crect.bottom = crect.top + tmp;
                }
            }
            else//horizon writing mode
            {
                tmp = crect.top;
                crect.top = (float)(PDFVContent.pgGetVY(vpage, crect.bottom) - m_content.vGetY());
                crect.bottom = (float)(PDFVContent.pgGetVY(vpage, tmp) - m_content.vGetY());
                tmp = (crect.bottom - crect.top) * 0.1f;
                if (PDFEditNode.caret_is_end(m_edit_pos1))
                {
                    crect.left = (float)(PDFVContent.pgGetVX(vpage, crect.right) - m_content.vGetX() - tmp * 0.5f);
                    crect.right = crect.left + tmp;
                }
                else
                {
                    crect.left = (float)(PDFVContent.pgGetVX(vpage, crect.left) - m_content.vGetX() - tmp * 0.5f);
                    crect.right = crect.left + tmp;
                }
            }
            m_edit_caret_rect = crect;
            m_edit_caret_sta |= 2;
        }
        private bool OnPageEditMove(Point point)
        {
            if (m_status != PDFV_STATUS.STA_EDIT_CONTENT) return false;
            PDFPos pos = m_content.vGetPos(point.X, point.Y);
            if (pos.pageno == m_annot_pos.pageno)
            {
                if (m_edit_pos0 == -1)//move block
                {
                    m_annot_rect.left = (float)(m_annot_rect0.left + point.X - m_hold_x);
                    m_annot_rect.top = (float)(m_annot_rect0.top + point.Y - m_hold_y);
                    m_annot_rect.right = (float)(m_annot_rect0.right + point.X - m_hold_x);
                    m_annot_rect.bottom = (float)(m_annot_rect0.bottom + point.Y - m_hold_y);
                }
                else
                {
                    m_edit_pos1 = m_edit_node.GetCharPos((float)pos.x, (float)pos.y);
                    onEditCaret();
                }
            }
            vDraw();
            return true;
        }
        /// <summary>
        /// touch end on rect status.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        private bool OnPageEditEnd(Point point)
        {
            if (m_status != PDFV_STATUS.STA_EDIT_CONTENT) return false;
            if (m_edit_node == null) return true;
            PDFPos pos = m_content.vGetPos(point.X, point.Y);
            if (pos.pageno == m_annot_pos.pageno)
            {
                if (m_edit_pos0 == -1)//move block
                {
                    m_annot_rect.left = (float)(m_annot_rect0.left + point.X - m_hold_x);
                    m_annot_rect.top = (float)(m_annot_rect0.top + point.Y - m_hold_y);
                    m_annot_rect.right = (float)(m_annot_rect0.right + point.X - m_hold_x);
                    m_annot_rect.bottom = (float)(m_annot_rect0.bottom + point.Y - m_hold_y);
                }
                else
                {
                    m_edit_pos1 = m_edit_node.GetCharPos((float)pos.x, (float)pos.y);
                    onEditCaret();
                }
            }
            RDRect nrect;
            nrect.left = (float)PDFVContent.pgToPDFX(m_annot_vpage, m_annot_rect.left, m_content.vGetX());
            nrect.top = (float)PDFVContent.pgToPDFY(m_annot_vpage, m_annot_rect.bottom, m_content.vGetY());
            nrect.right = (float)PDFVContent.pgToPDFX(m_annot_vpage, m_annot_rect.right, m_content.vGetX());
            nrect.bottom = (float)PDFVContent.pgToPDFY(m_annot_vpage, m_annot_rect.top, m_content.vGetY());
            m_edit_node.Rect = nrect;
            if (m_edit_node.Type != 1) m_modified = true;
            //String ssel = m_edit_node.TextGetString(m_edit_pos0, m_edit_pos1);
            m_annot_page.SetPGEditorModified(true);
            m_content.vRenderPage(m_annot_vpage);
            vDraw();
            m_parent.Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => { m_ctrl.Focus(FocusState.Keyboard); });
            return true;
        }

        private int FTSPage = -1;
        private RDRect FTSRect;

        bool manualScroll = false;
        private void DrawFTS()
        {
            if (FTSPage > -1 && m_pageno == FTSPage)
            {
                long vpage = m_content.vGetPage(FTSPage);
                double scrollx = to_contx(m_scroller.HorizontalOffset);
                double scrolly = to_conty(m_scroller.VerticalOffset);
                RDRect ftsViewRect;
                ftsViewRect.left = (float)(PDFVContent.pgGetVX(vpage, FTSRect.left) - scrollx);
                ftsViewRect.right = (float)(PDFVContent.pgGetVX(vpage, FTSRect.right) - scrollx);
                ftsViewRect.top = (float)(PDFVContent.pgGetVY(vpage, FTSRect.bottom) - scrolly);
                ftsViewRect.bottom = (float)(PDFVContent.pgGetVY(vpage, FTSRect.top) - scrolly);

                m_canvas.draw_rect(ftsViewRect, 2, 0x80000000);
                m_canvas.fill_rect(ftsViewRect, Color.FromArgb(0x40, 0, 0, 255));
                if (manualScroll)
                { //needed to overcome stack overflow issue, as ChangeView caused Draw method to be recalled
                    manualScroll = false;
                    return;
                }
                //check if we need to scroll to the FTS position
                if ((m_content.vGetY() + ftsViewRect.bottom > m_scroller.VerticalOffset + m_scroller.ActualHeight && ftsViewRect.bottom - ftsViewRect.top
                    < m_scroller.ActualHeight) || m_scroller.VerticalOffset + ftsViewRect.top > m_scroller.VerticalOffset + m_scroller.ActualHeight)
                {
                    manualScroll = true;
                    double vertOffset = ftsViewRect.bottom - m_scroller.ActualHeight;
                    m_scroller.ChangeView(m_scroller.HorizontalOffset, m_scroller.VerticalOffset + vertOffset + 10, m_scroller.ZoomFactor, true);
                }
                else if (ftsViewRect.top < 0)
                {
                    manualScroll = true;
                    m_scroller.ChangeView(m_scroller.HorizontalOffset, m_scroller.VerticalOffset + ftsViewRect.top - 10, m_scroller.ZoomFactor, true);
                }
            }
        }

        public void PDFSetFTSRect(Object rect, int pageno)
        {
            FTSPage = pageno;
            if (rect != null)
            {
                FTSRect = (RDRect)rect;
            }
            vDraw();
        }

        public double GetScale()
        {
            return m_scale;
        }
    }



    public class PDFThumb : IContentListener
    {
        public PDFThumb()
        {
            m_parent = null;
            m_scroller = new ScrollViewer();
            m_content = new PDFVContent();
            m_scroller.Content = m_content;
            m_page_sel = -1;

            m_doc = null;
            m_sel = null;
            m_touched = false;
        }
        ~PDFThumb()
        {
            PDFClose();
        }
        public void PDFSaveView()
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
        public void PDFRestoreView()
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
        public bool PDFOpen(RelativePanel parent, PDFDoc doc, IThumbListener listener)
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

            m_content.vOpen(m_doc, (RDLAYOUT_MODE)100, this);//horizon thumbnail view
            //m_content.vOpen(m_doc, (RDLAYOUT_MODE)101, this);//vertical thumbnail view
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
        public void PDFSetSelPage(int pageno)
        {
            m_content.vSetPageSel(pageno);
            vDraw();
        }
        public void PDFUpdatePage(int pageno)
        {
            long vpage = m_content.vGetPage(pageno);
            if (vpage != 0)
            {
                m_content.vRenderPage(vpage);
                vDraw();
            }
        }
        public void PDFClose()
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
            m_scroller.ChangeView(vx, vy, (float)m_content.vGetScale(), false);

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
        private PDFDoc m_doc;
        private ScrollViewer m_scroller;
        private PDFVContent m_content;
        private RelativePanel m_parent;
        private Rectangle m_sel;
        private IThumbListener m_listener;

    }
}
