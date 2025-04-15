#pragma once
#include "RDDOCX.h"
#include "RDDOCXVLayout.h"
#include "RDDOCXVSel.h"
#include "RDDOCXVCallback.h"
#include "RDViewer.h"
#include <Windows.h>
using namespace RDDLib::docx;
using namespace RDDLib::docxv;
using namespace Windows::Foundation;
using namespace Windows::UI::Xaml;
using namespace Windows::UI::Xaml::Media;
using namespace Windows::UI::Xaml::Controls;
using namespace Windows::UI::Xaml::Input;
using namespace Windows::UI::Xaml::Shapes;
using namespace Windows::UI::Input;

namespace RDDLib
{
	namespace reader
	{
		public ref class DOCXVCanvas sealed: public Canvas
		{
		public:
			DOCXVCanvas() : Canvas()
			{
				IsHitTestVisible = false;
				Visibility = Windows::UI::Xaml::Visibility::Visible;
			}
			virtual ~DOCXVCanvas()
			{
				clear();
			}
			/// <summary>
			/// Draw a rectangle onto the canvas and fill it with specified color
			/// </summary>
			/// <param name="rect">The rectangle to draw</param>
			/// <param name="clr">The color will be used to fill the rectangle</param>
			void fill_rect(RDRect rect, Windows::UI::Color clr)
			{
				Rectangle^ rect1 = ref new Rectangle();
				rect1->SetValue(Canvas::LeftProperty, rect.left);
				rect1->SetValue(Canvas::TopProperty, rect.top);
				rect1->Width = (rect.right - rect.left);
				rect1->Height = (rect.bottom - rect.top);
				rect1->Fill = ref new SolidColorBrush(clr);
				Children->Append(rect1);
			}
			/// <summary>
			/// Draw a rectangle onto the canvas
			/// </summary>
			/// <param name="rect">The rectangle to draw</param>
			/// <param name="width">The border width of the rectangle</param>
			/// <param name="color">The color of the border</param>
			void draw_rect(RDRect rect, float width, unsigned int color)
			{
				Rectangle^ rect1 = ref new Rectangle();
				Windows::UI::Color clr;
				clr.A = (byte)(color >> 24);
				clr.R = (byte)(color >> 16);
				clr.G = (byte)(color >> 8);
				clr.B = (byte)(color);
				rect1->Stroke = ref new SolidColorBrush(clr);
				rect1->StrokeThickness = width;
				rect1->SetValue(Canvas::LeftProperty, rect.left);
				rect1->SetValue(Canvas::TopProperty, rect.top);
				rect1->Width = rect.right - rect.left;
				rect1->Height = rect.bottom - rect.top;
				Children->Append(rect1);
			}
			/// <summary>
			/// Draw a ink (doodle) onto the canvas
			/// </summary>
			/// <param name="ink">The ink(doodle) data to draw</param>
			/// <param name="path">The path data of the ink object</param>
			/// <param name="pos"></param>
			/// <param name="offsetx"></param>
			/// <param name="offsety"></param>
			/// <returns></returns>
			int draw_ink(RDInk^ ink, Path^ path, int pos, float offsetx, float offsety)
			{
				int cur = 0;
				int cnt = ink->NodesCnt;
				RDPoint pt;
				PathGeometry^ inkg = (PathGeometry^)path->Data;
				PathFigure^ inkf = inkg->Figures->GetAt(inkg->Figures->Size - 1);
				LineSegment^ line;
				Windows::UI::Xaml::Media::BezierSegment^ bezier;
				Point ppt;
				for (cur = pos; cur < cnt; cur++)
				{
					switch (ink->GetOP(cur))
					{
					case 0:
						pt = ink->GetPoint(cur);
						ppt.X = pt.x + offsetx;
						ppt.Y = pt.y + offsety;
						inkf->StartPoint = ppt;
						break;
					case 1:
						pt = ink->GetPoint(cur);
						line = ref new LineSegment();
						ppt.X = pt.x + offsetx;
						ppt.Y = pt.y + offsety;
						line->Point = ppt;
						inkf->Segments->Append(line);
						break;
					case 2:
						pt = ink->GetPoint(cur);
						bezier = ref new Windows::UI::Xaml::Media::BezierSegment();
						ppt.X = pt.x + offsetx;
						ppt.Y = pt.y + offsety;
						bezier->Point1 = ppt;
						bezier->Point2 = ppt;
						pt = ink->GetPoint(cur + 1);
						ppt.X = pt.x + offsetx;
						ppt.Y = pt.y + offsety;
						bezier->Point3 = ppt;
						cur++;
						inkf->Segments->Append(bezier);
						break;
					}
				}
				Children->Append(path);
				return cnt;
			}
			/// <summary>
			/// Draw a polygon to the view
			/// </summary>
			/// <param name="polygon">A RDPath which contains drawing operations</param>
			/// <param name="path">A Path object which is used stores the polygon data</param>
			/// <param name="pos">Index of operation which should be started from</param>
			/// <param name="offsetx">Offset on x coordinate</param>
			/// <param name="offsety">Offset on y coordinate</param>
			/// <returns>Node count of the ploygon</returns>
			int draw_polygon(RDPath^ polygon, Path^ path, int pos, float offsetx, float offsety)
			{
				int cur = 0;
				int cnt = polygon->NodesCnt;
				RDPoint pt;
				PathGeometry^ inkg = (PathGeometry^)path->Data;
				PathFigure^ inkf = inkg->Figures->GetAt(0);
				LineSegment^ line;
				Point ppt;
				Point ppts;
				Windows::Foundation::Size dotsz;
				dotsz.Width = 10;
				dotsz.Height = 10;
				for (cur = pos; cur < cnt; cur++)
				{
					switch (polygon->GetOP(cur))
					{
					case 0:
						pt = polygon->GetPoint(cur);
						ppt.X = pt.x + offsetx;
						ppt.Y = pt.y + offsety;
						inkf->StartPoint = ppt;
						break;
					case 1:
						pt = polygon->GetPoint(cur);
						line = ref new LineSegment();
						ppt.X = pt.x + offsetx;
						ppt.Y = pt.y + offsety;
						line->Point = ppt;
						inkf->Segments->Append(line);
						break;
					}
					PathFigure^ dotc = ref new PathFigure();
					ppts = ppt;
					ppts.X += 10;
					dotc->StartPoint = ppts;
					Windows::UI::Xaml::Media::ArcSegment^ dot = ref new Windows::UI::Xaml::Media::ArcSegment();
					dot->IsLargeArc = true;
					ppts.Y += 0.01;
					dot->Point = ppts;
					dot->Size = dotsz;
					//dot->SweepDirection = SweepDirection::Clockwise;
					dotc->Segments->Append(dot);
					dotc->IsClosed = true;
					inkg->Figures->Append(dotc);
				}
				Children->Append(path);
				return cnt;
			}
			/// <summary>
			/// Draw a polyline to the view
			/// </summary>
			/// <param name="polygon">A RDPath which contains drawing operations</param>
			/// <param name="path">A Path object which is used stores the polyline data</param>
			/// <param name="pos">Index of operation which should be started from</param>
			/// <param name="offsetx">Offset on x coordinate</param>
			/// <param name="offsety">Offset on y coordinate</param>
			/// <returns>Node count of the ploygon</returns>
			int draw_polyline(RDPath^ polygon, Path^ path, int pos, float offsetx, float offsety)
			{
				int cur = 0;
				int cnt = polygon->NodesCnt;
				RDPoint pt;
				PathGeometry^ inkg = (PathGeometry^)path->Data;
				PathFigure^ inkf = inkg->Figures->GetAt(0);
				LineSegment^ line;
				Point ppt;
				Point ppts;
				Windows::Foundation::Size dotsz;
				dotsz.Width = 10;
				dotsz.Height = 10;
				for (cur = pos; cur < cnt; cur++)
				{
					switch (polygon->GetOP(cur))
					{
					case 0:
						pt = polygon->GetPoint(cur);
						ppt.X = pt.x + offsetx;
						ppt.Y = pt.y + offsety;
						inkf->StartPoint = ppt;
						break;
					case 1:
						pt = polygon->GetPoint(cur);
						line = ref new LineSegment();
						ppt.X = pt.x + offsetx;
						ppt.Y = pt.y + offsety;
						line->Point = ppt;
						inkf->Segments->Append(line);
						break;
					}
					PathFigure^ dotc = ref new PathFigure();
					ppts = ppt;
					ppts.X += 10;
					dotc->StartPoint = ppts;
					Windows::UI::Xaml::Media::ArcSegment^ dot = ref new Windows::UI::Xaml::Media::ArcSegment();
					dot->IsLargeArc = true;
					ppts.Y += 0.01;
					dot->Point = ppts;
					dot->Size = dotsz;
					dotc->Segments->Append(dot);
					dotc->IsClosed = true;
					inkg->Figures->Append(dotc);
				}
				Children->Append(path);
				return cnt;
			}
			/// <summary>
			/// Draw a stamp to the view
			/// </summary>
			/// <param name="pts">An array of points describes the position and size of the stamp</param>
			/// <param name="cnt">Count of points</param>
			/// <param name="bmp">A WriteableBitmap of the stamp image</param>
			/// <param name="offsetx">Offset on x coordinate</param>
			/// <param name="offsety">Offset on y coordinate</param>
			void draw_stamps(const Array <RDPoint>^ pts, int cnt, WriteableBitmap ^bmp, float offsetx, float offsety)
			{
				int cur = 0;
				cnt <<= 1;
				for (cur = 0; cur < cnt; cur += 2)
				{
					RDPoint pt0 = pts[cur];
					RDPoint pt1 = pts[cur + 1];
					Image^ rect = ref new Image();
					if (pt0.x > pt1.x)
					{
						rect->SetValue(Canvas::LeftProperty, pt1.x + offsetx);
						rect->Width = pt0.x - pt1.x;
					}
					else
					{
						rect->SetValue(Canvas::LeftProperty, pt0.x + offsetx);
						rect->Width = pt1.x - pt0.x;
					}
					if (pt0.y > pt1.y)
					{
						rect->SetValue(Canvas::TopProperty, pt1.y + offsety);
						rect->Height = pt0.y - pt1.y;
					}
					else
					{
						rect->SetValue(Canvas::TopProperty, pt0.y + offsety);
						rect->Height = pt1.y - pt0.y;
					}
					rect->Stretch = Stretch::Fill;
					rect->Source = bmp;
					Children->Append(rect);
				}
			}
			/// <summary>
			/// Draw a Rectangle to the view
			/// </summary>
			/// <param name="pts">An array of RDPoint objects describes the position and size of the rectangle</param>
			/// <param name="cnt">Count of points</param>
			/// <param name="width">Line width of the rectangle</param>
			/// <param name="color">Color of the line</param>
			/// <param name="offsetx">Offset on x coordinate</param>
			/// <param name="offsety">Offset on y coordinate</param>
			void draw_rects(const Array <RDPoint> ^pts, int cnt, float width, unsigned int color, float offsetx, float offsety)
			{
				Windows::UI::Color clr;
				clr.A = (byte)(color >> 24);
				clr.R = (byte)(color >> 16);
				clr.G = (byte)(color >> 8);
				clr.B = (byte)(color);
				SolidColorBrush^ br = ref new SolidColorBrush(clr);
				int cur = 0;
				cnt <<= 1;
				for (cur = 0; cur < cnt; cur += 2)
				{
					RDPoint pt0 = pts[cur];
					RDPoint pt1 = pts[cur + 1];
					Rectangle^ rect = ref new Rectangle();
					rect->StrokeThickness = width;
					rect->Stroke = br;
					if (pt0.x > pt1.x)
					{
						rect->SetValue(Canvas::LeftProperty, pt1.x + offsetx);
						rect->Width = pt0.x - pt1.x;
					}
					else
					{
						rect->SetValue(Canvas::LeftProperty, pt0.x + offsetx);
						rect->Width = pt1.x - pt0.x;
					}
					if (pt0.y > pt1.y)
					{
						rect->SetValue(Canvas::TopProperty, pt1.y + offsety);
						rect->Height = pt0.y - pt1.y;
					}
					else
					{
						rect->SetValue(Canvas::TopProperty, pt0.y + offsety);
						rect->Height = pt1.y - pt0.y;
					}
					Children->Append(rect);
				}
			}
			/// <summary>
			/// Draw a ellipse to the view
			/// </summary>
			/// <param name="pts">An array of RDPoint objects describes the position and size of the ellipse</param>
			/// <param name="cnt">Count of points</param>
			/// <param name="width">Line width of the rectangle</param>
			/// <param name="color">Color of the line</param>
			/// <param name="offsetx">Offset on x coordinate</param>
			/// <param name="offsety">Offset on y coordinate</param>
			void draw_ovals(const Array <RDPoint> ^pts, int cnt, float width, unsigned int color, float offsetx, float offsety)
			{
				Windows::UI::Color clr;
				clr.A = (byte)(color >> 24);
				clr.R = (byte)(color >> 16);
				clr.G = (byte)(color >> 8);
				clr.B = (byte)(color);
				SolidColorBrush^ br = ref new SolidColorBrush(clr);
				int cur = 0;
				cnt <<= 1;
				float w = width * 0.5f;
				for (cur = 0; cur < cnt; cur += 2)
				{
					RDPoint pt0 = pts[cur];
					RDPoint pt1 = pts[cur + 1];
					Windows::UI::Xaml::Shapes::Ellipse^ rect = ref new Windows::UI::Xaml::Shapes::Ellipse();
					rect->StrokeThickness = width;
					rect->Stroke = br;
					if (pt0.x > pt1.x)
					{
						rect->SetValue(Canvas::LeftProperty, pt1.x - w + offsetx);
						rect->Width = pt0.x - pt1.x + width;
					}
					else
					{
						rect->SetValue(Canvas::LeftProperty, pt0.x - w + offsetx);
						rect->Width = pt1.x - pt0.x + width;
					}
					if (pt0.y > pt1.y)
					{
						rect->SetValue(Canvas::TopProperty, pt1.y - w + offsety);
						rect->Height = pt0.y - pt1.y + width;
					}
					else
					{
						rect->SetValue(Canvas::TopProperty, pt0.y - w + offsety);
						rect->Height = pt1.y - pt0.y + width;
					}
					Children->Append(rect);
				}
			}
			/// <summary>
			/// Draw lines to the view
			/// </summary>
			/// <param name="pts">An array of RDPoint describes the lines</param>
			/// <param name="cnt">Count the points</param>
			/// <param name="width">Line width</param>
			/// <param name="color">Line color</param>
			/// <param name="offsetx">Offset on x coordinate</param>
			/// <param name="offsety">Offset on y coordinate</param>
			void draw_lines(const Array<RDPoint> ^pts, int cnt, float width, unsigned int color, float offsetx, float offsety)
			{
				Windows::UI::Color clr;
				clr.A = (byte)(color >> 24);
				clr.R = (byte)(color >> 16);
				clr.G = (byte)(color >> 8);
				clr.B = (byte)(color);
				SolidColorBrush^ br = ref new SolidColorBrush(clr);
				int cur = 0;
				cnt <<= 1;
				float w = width * 0.5f;
				for (cur = 0; cur < cnt; cur += 2)
				{
					RDPoint pt0 = pts[cur];
					RDPoint pt1 = pts[cur + 1];
					Line^ line = ref new Line();
					line->StrokeThickness = width;
					line->Stroke = br;
					line->SetValue(Line::X1Property, pt0.x + offsetx);
					line->SetValue(Line::X2Property, pt1.x + offsetx);
					line->SetValue(Line::Y1Property, pt0.y + offsety);
					line->SetValue(Line::Y2Property, pt1.y + offsety);
					Children->Append(line);
				}
			}
			void clear()
			{
				Children->Clear();
			}
		private:
		internal:
			void draw_rects(RDPoint* pts, int cnt, float width, unsigned int color, float offsetx, float offsety)
			{
				Windows::UI::Color clr;
				clr.A = (byte)(color >> 24);
				clr.R = (byte)(color >> 16);
				clr.G = (byte)(color >> 8);
				clr.B = (byte)(color);
				SolidColorBrush^ br = ref new SolidColorBrush(clr);
				int cur = 0;
				cnt <<= 1;
				for (cur = 0; cur < cnt; cur += 2)
				{
					RDPoint pt0 = pts[cur];
					RDPoint pt1 = pts[cur + 1];
					Rectangle^ rect = ref new Rectangle();
					rect->StrokeThickness = width;
					rect->Stroke = br;
					if (pt0.x > pt1.x)
					{
						rect->SetValue(Canvas::LeftProperty, pt1.x + offsetx);
						rect->Width = pt0.x - pt1.x;
					}
					else
					{
						rect->SetValue(Canvas::LeftProperty, pt0.x + offsetx);
						rect->Width = pt1.x - pt0.x;
					}
					if (pt0.y > pt1.y)
					{
						rect->SetValue(Canvas::TopProperty, pt1.y + offsety);
						rect->Height = pt0.y - pt1.y;
					}
					else
					{
						rect->SetValue(Canvas::TopProperty, pt0.y + offsety);
						rect->Height = pt1.y - pt0.y;
					}
					Children->Append(rect);
				}
			}
			void draw_ovals(RDPoint* pts, int cnt, float width, unsigned int color, float offsetx, float offsety)
			{
				Windows::UI::Color clr;
				clr.A = (byte)(color >> 24);
				clr.R = (byte)(color >> 16);
				clr.G = (byte)(color >> 8);
				clr.B = (byte)(color);
				SolidColorBrush^ br = ref new SolidColorBrush(clr);
				int cur = 0;
				cnt <<= 1;
				float w = width * 0.5f;
				for (cur = 0; cur < cnt; cur += 2)
				{
					RDPoint pt0 = pts[cur];
					RDPoint pt1 = pts[cur + 1];
					Windows::UI::Xaml::Shapes::Ellipse^ rect = ref new Windows::UI::Xaml::Shapes::Ellipse();
					rect->StrokeThickness = width;
					rect->Stroke = br;
					if (pt0.x > pt1.x)
					{
						rect->SetValue(Canvas::LeftProperty, pt1.x - w + offsetx);
						rect->Width = pt0.x - pt1.x + width;
					}
					else
					{
						rect->SetValue(Canvas::LeftProperty, pt0.x - w + offsetx);
						rect->Width = pt1.x - pt0.x + width;
					}
					if (pt0.y > pt1.y)
					{
						rect->SetValue(Canvas::TopProperty, pt1.y - w + offsety);
						rect->Height = pt0.y - pt1.y + width;
					}
					else
					{
						rect->SetValue(Canvas::TopProperty, pt0.y - w + offsety);
						rect->Height = pt1.y - pt0.y + width;
					}
					Children->Append(rect);
				}
			}
			void draw_lines(RDPoint* pts, int cnt, float width, unsigned int color, float offsetx, float offsety)
			{
				Windows::UI::Color clr;
				clr.A = (byte)(color >> 24);
				clr.R = (byte)(color >> 16);
				clr.G = (byte)(color >> 8);
				clr.B = (byte)(color);
				SolidColorBrush^ br = ref new SolidColorBrush(clr);
				int cur = 0;
				cnt <<= 1;
				float w = width * 0.5f;
				for (cur = 0; cur < cnt; cur += 2)
				{
					RDPoint pt0 = pts[cur];
					RDPoint pt1 = pts[cur + 1];
					Line^ line = ref new Line();
					line->StrokeThickness = width;
					line->Stroke = br;
					line->SetValue(Line::X1Property, pt0.x + offsetx);
					line->SetValue(Line::X2Property, pt1.x + offsetx);
					line->SetValue(Line::Y1Property, pt0.y + offsety);
					line->SetValue(Line::Y2Property, pt1.y + offsety);
					Children->Append(line);
				}
			}
		};

		public interface class IDOCXViewListener
		{
			void OnDOCXScaleChanged(double scale);
			/// <summary>
			/// fired when page no changed.
			/// </summary>
			/// <param name="pageno">page NO.</param>
			void OnDOCXPageChanged(int pageno);
			/// <summary>
			/// fired when single tapped on page, without annotations.
			/// </summary>
			/// <param name="x"></param>
			/// <param name="y"></param>
			/// <returns></returns>
			void OnDOCXSingleTapped(float x, float y);
			void OnDOCXPageTapped(int pageno);
			void OnDOCXLongPressed(float x, float y);
			/// <summary>
			/// fired when text found.
			/// </summary>
			/// <param name="found">true if found, false if end of document.</param>
			void OnDOCXFound(Boolean found);
			/// <summary>
			/// fired when selecting.
			/// </summary>
			/// <param name="canvas">Canvas object to draw.</param>
			/// <param name="rect1">first char's location, in Canvas coordinate.</param>
			/// <param name="rect2">last char's location, in Canvas coordinate.</param>
			void OnDOCXSelecting(Canvas^ canvas, RDRect rect1, RDRect rect2);
			/// <summary>
			/// fired when text selected.
			/// using vGetSelText to get texts.
			/// using vSelEnd to end selecting status.
			/// using vSelMarkup to set markup annotation.
			/// </summary>
			void OnDOCXSelected();
			/// <summary>
			/// fired when uri link annotation performed.
			/// </summary>
			/// <param name="uri">uri address.</param>
			void OnDOCXURI(String^ uri);
		};
		//all coordinate transform done in this class.
		public ref class DOCXVContent sealed : public Canvas, IVCallback
		{
		public:
			DOCXVContent() :Canvas()
			{
				Windows::Graphics::Display::DisplayInformation^ disp = Windows::Graphics::Display::DisplayInformation::GetForCurrentView();
				m_doc = nullptr;
				m_layout = NULL;
				m_zoom = 1;
				m_bound = nullptr;
				m_page_sel = -1;
			}
			virtual ~DOCXVContent()
			{
				vClose();
			}
			/// <summary>
			/// Get scale of the content
			/// </summary>
			/// <returns>The scale</returns>
			inline double vGetScale()
			{
				if (m_layout) return m_layout->vGetScale();
				else return -1;
			}
			/// <summary>
			/// Get DOCXPos base on specified x and y coordinates on view element.
			/// </summary>
			/// <param name="vx">x coordinate on view element</param>
			/// <param name="vy">y coordinate on view element</param>
			/// <returns></returns>
			inline DOCXPos vGetPos(double vx, double vy)
			{
				if (m_layout) return m_layout->vGetPos(vx, vy);
				DOCXPos pos;
				pos.pageno = -1;
				pos.x = 0;
				pos.y = 0;
				return pos;
			}
			/// <summary>
			/// Check if it is possible to set position of the content 
			/// </summary>
			/// <returns></returns>
			inline bool vCanSetPos()
			{
				return (m_layout->vGetLayW() > 0 && m_layout->vGetLayH() > 0);
			}
			/// <summary>
			/// Set and jump to specified position
			/// </summary>
			/// <param name="vx">offset on x coordinate on view component</param>
			/// <param name="vy">offset on y coordinate on view component</param>
			/// <param name="pos">A PDFPos object to specify where the content should be placed on the PDF document</param>
			inline void vSetPos(double vx, double vy, const DOCXPos& pos)
			{
				if (m_layout)
				{
					m_layout->vSetPos(vx, vy, pos);
					m_callback->cSetPos(m_layout->vGetX(), m_layout->vGetY());
				}
			}
			/// <summary>
			/// Get view x of the content
			/// </summary>
			/// <returns>x coordinate on view content</returns>
			inline double vGetX()
			{
				if (m_layout) return m_layout->vGetX();
				return 0;
			}
			/// <summary>
			/// Get view y of the content
			/// </summary>
			/// <returns>y coordinate on view content</returns>
			inline double vGetY()
			{
				if (m_layout) return m_layout->vGetY();
				return 0;
			}
			/// <summary>
			/// Set selected page
			/// </summary>
			/// <param name="pageno">0 base page index</param>
			inline void vSetPageSel(int pageno)
			{
				if (!m_layout || !m_doc || m_page_sel == pageno || pageno < 0)
					return;
				m_page_sel = pageno;
				CRDVPage* vpage = m_layout->vGetPage(pageno);
				DOCXPos pos;
				pos.pageno = m_page_sel;
				pos.x = 0.5 * m_doc->GetPageWidth(m_page_sel);
				pos.y = 0.5 * m_doc->GetPageHeight(m_page_sel);
				m_layout->vSetPos(m_layout->vGetVW() * 0.5, m_layout->vGetVH() * 0.5, pos);
				if (m_callback)
				{
					m_callback->cSetPos(m_layout->vGetX(), m_layout->vGetY());
					RDRect rect;
					rect.left = vpage->GetLeft();
					rect.top = vpage->GetTop();
					rect.right = vpage->GetRight();
					rect.bottom = vpage->GetBottom();
					Windows::UI::Color clr;
					clr.A = 64;
					clr.B = 224;
					clr.G = 0;
					clr.R = 0;
					m_callback->cFillRect(rect, clr);
				}
			}
			/// <summary>
			/// Get index of selected page
			/// </summary>
			/// <returns>Index of selected page</returns>
			inline int vGetPageSel()
			{
				return m_page_sel;
			}
			/// <summary>
			/// Get a reference of a DOCX view page.
			/// </summary>
			/// <returns>Reference of page</returns>
			inline long long vGetPage(int pgno)
			{
				if (m_layout) return (long long)m_layout->vGetPage(pgno);
				return NULL;
			}
			/// <summary>
			/// Get a reference of a DOCX view page with specified coordinate
			/// </summary>
			/// <param name="vx">x coordinate on view component</param>
			/// <param name="vy">y coordinate on view component</param>
			/// <returns>Reference of page</returns>
			inline int vGetPage(double vx, double vy)
			{
				if (m_layout) return m_layout->vGetPage(vx, vy);
				return -1;
			}
			/// <summary>
			/// Open and display a opened PDFDoc
			/// </summary>
			/// <param name="doc">Opened PDFDoc to display on the view</param>
			/// <param name="lmode">View mode for displaying, please refer to definition of PDF_LAYOUT_MODE</param>
			/// <param name="callback">Callcall to deal with graphic tasks. Please refer to definition of IPDFContentListener</param>
			/// <returns>True if successed, otherwise false</returns>
			inline bool vOpen(DOCXDoc^ doc, RDLAYOUT_MODE lmode, IContentListener ^callback)
			{
				if (!doc) return false;
				m_doc = doc;
				m_callback = callback;
				vSetView(lmode);
				m_page_sel = -1;
				return true;
			}
			/// <summary>
			/// Set and update view mode
			/// </summary>
			/// <param name="vmode">New view mode, please refer to definition of PDF_LAYOUT_MODE</param>
			inline void vSetView(RDLAYOUT_MODE vmode)
			{
				if (!m_doc) return;
				if (m_layout)
				{
					m_layout->vClose();
					m_layout = NULL;
				}
				switch (vmode)
				{
				case RDLAYOUT_MODE::layout_vert:
					m_layout = new CRDVLayoutVert(CRDVLayoutVert::ALIGN_CENTER, false);
					break;
				case RDLAYOUT_MODE::layout_horz:
					m_layout = new CRDVLayoutHorz(CRDVLayoutHorz::ALIGN_CENTER, false);
					break;
				case RDLAYOUT_MODE::layout_dual:
					m_layout = new CRDVLayoutDual(CRDVLayoutDual::SCALE_NONE, false, false);
					break;
				case RDLAYOUT_MODE::layout_dual_cover:
					m_layout = new CRDVLayoutDual(CRDVLayoutDual::SCALE_NONE, false, true);
					break;
				case RDLAYOUT_MODE::layout_hsingle:
					m_layout = new CRDVLayoutDualH(CRDVLayoutDualH::SCALE_NONE, CRDVLayoutDualH::ALIGN_CENTER, false, NULL, 0, NULL, 0);
					break;
				case RDLAYOUT_MODE::layout_hdual:
				{
					bool* bhorz = (bool *)malloc(sizeof(bool) * m_doc->PageCount);
					memset(bhorz, 1, sizeof(bool) * m_doc->PageCount);
					m_layout = new CRDVLayoutDualH(CRDVLayoutDualH::SCALE_NONE, CRDVLayoutDualH::ALIGN_CENTER, false, NULL, 0, bhorz, m_doc->PageCount);
					free(bhorz);
				}
					break;
				case (RDLAYOUT_MODE)100:
					m_layout = new CRDVLayoutThumb(CRDVLayoutThumb::ALIGN_CENTER, false);
					break;
				default:
					m_layout = new CRDVLayoutVert(CRDVLayoutVert::ALIGN_CENTER, true);
					break;
				}
				m_layout->vOpen(m_doc, this, 4);
				InvalidateMeasure();
			}
			/// <summary>
			/// Set if auto resize the PDF pages to fit view size.
			/// </summary>
			/// <param name="autofit"></param>
			inline void vSetAutoFit(bool autofit)
			{
				if (!m_layout) return;
				m_layout->vSetAutoFit(autofit);
			}
			/// <summary>
			/// Check if auto fit view size is set
			/// </summary>
			/// <returns>True or false</returns>
			inline bool vGetAutoFit()
			{
				if (!m_layout) return false;
				return m_layout->vGetAutoFit();
			}
			/// <summary>
			/// Resize the view
			/// </summary>
			/// <param name="vw">New view width</param>
			/// <param name="vh">New view height</param>
			/// <param name="scale">PDF page scale</param>
			inline void vResize(double vw, double vh, double scale)
			{
				m_zoom = scale;
				if (m_layout)
				{
					m_layout->vResize(vw, vh);
					if(m_layout->vGetAutoFit())
						m_zoom = m_layout->vGetScale();//update zoom value to autofit value.
					if (m_page_sel >= 0)
					{
						int pgno = m_page_sel;
						m_page_sel = -1;
						vSetPageSel(pgno);
					}
					InvalidateMeasure();
				}
			}
			/// <summary>
			/// Draw on view
			/// </summary>
			/// <param name="vx">x coordinate to draw on view</param>
			/// <param name="vy">y coordinate to draw on view</param>
			inline void vDraw(double vx, double vy)
			{
				if (m_layout)
				{
					m_layout->vSetX(vx);
					m_layout->vSetY(vy);
					m_layout->vDraw();
				}
			}
			/// <summary>
			/// Draw highlight background mark the text found during search
			/// </summary>
			/// <param name="vx">x coordinate of the top left point of highlight area</param>
			/// <param name="vy">y coordinate of the top left point of highlight area</param>
			inline void vDrawFind(double vx, double vy)
			{
				if (m_layout)
					m_layout->vDrawFind(vx, vy);
			}
			/// <summary>
			/// Activate text search mode
			/// </summary>
			/// <param name="pat">Texts to search</param>
			/// <param name="match_case">True if search with case sensitive, otherwise false</param>
			/// <param name="whole_word">True if search with whole word match, otherwise false</param>
			inline void vFindStart(String^ pat, bool match_case, bool whole_word)
			{
				if (m_layout)
					m_layout->vFindStart(pat, match_case, whole_word);
			}
			/// <summary>
			/// Find next match to texts set in vFindStart
			/// </summary>
			/// <param name="dir">Direction to search. if dir < 0, search backward, otherwise search forward</param>
			inline void vFind(int dir)
			{
				if (m_layout)
					m_layout->vFind(dir);
			}
			/// <summary>
			/// Deactivate text search mode
			/// </summary>
			inline void vFindEnd()
			{
				if (m_layout)
					m_layout->vFindEnd();
			}
			/// <summary>
			/// Close view and release holding resources
			/// </summary>
			inline void vClose()
			{
				if (!m_doc) return;
				m_bound = nullptr;
				if (m_layout)
				{
					m_layout->vClose();
					//delete in delegate callback
					m_layout = NULL;
				}
				m_callback = nullptr;
				Children->Clear();
				m_doc = nullptr;
				m_page_sel = -1;
			}
			/// <summary>
			/// Activate zoom mode
			/// </summary>
			inline void vZoomStart()
			{
				if (!m_layout) return;
				m_layout->vZoomStart();
			}
			/// <summary>
			/// Deactivate zoom mode
			/// </summary>
			inline void vZoomConfirm()
			{
				if (!m_layout) return;
				m_layout->vZoomConfirm();
			}
			/// <summary>
			/// Set scale and zoom in/out current displaying content
			/// </summary>
			/// <param name="scale">zoom scale</param>
			inline void vZoomSet(double scale)
			{
				if (!m_layout) return;
				m_zoom = scale;
				m_layout->vZoomSet(scale);
			}
			/// <summary>
			/// Render a PDF page on view
			/// </summary>
			/// <param name="page">Reference of a PDF page to render</param>
			inline void vRenderPage(long long page)
			{
				if (!page) return;
				((CRDVPage *)page)->ui_set_dirty();
			}
			static inline int pgGetPageNo(long long vpage) { return ((CRDVPage*)vpage)->GetPageNo(); }
			static inline int pgGetLeft(long long vpage) { return ((CRDVPage*)vpage)->GetLeft(); }
			static inline int pgGetTop(long long vpage) { return ((CRDVPage*)vpage)->GetTop(); }
			static inline int pgGetRight(long long vpage) { return ((CRDVPage*)vpage)->GetRight(); }
			static inline int pgGetBottom(long long vpage) { return ((CRDVPage*)vpage)->GetBottom(); }
			static inline int pgGetWidth(long long vpage) { return ((CRDVPage*)vpage)->GetWidth(); }
			static inline int pgGetHeight(long long vpage) { return ((CRDVPage*)vpage)->GetHeight(); }
			static inline double pgGetScale(long long vpage) { return ((CRDVPage*)vpage)->GetScale(); }
			static inline double pgGetPDFX(long long vpage, double vx) { return ((CRDVPage*)vpage)->GetPDFX(vx); }
			static inline double pgGetPDFY(long long vpage, double vy) { return ((CRDVPage*)vpage)->GetPDFY(vy); }
			static inline double pgGetVX(long long vpage, double pdfx) { return ((CRDVPage*)vpage)->GetVX(pdfx); }
			static inline double pgGetVY(long long vpage, double pdfy) { return ((CRDVPage*)vpage)->GetVY(pdfy); }
			static inline double pgToDIBX(long long vpage, double x) { return ((CRDVPage*)vpage)->ToDIBX(x); }
			static inline double pgToDIBY(long long vpage, double y) { return ((CRDVPage*)vpage)->ToDIBY(y); }
			static inline RDMatrix^ pgCreateInvertMatrix(long long vpage, double scrollx, double scrolly)
			{
				return ((CRDVPage*)vpage)->CreateInvertMatrix(scrollx, scrolly);
			}
		public:
			/// <summary>
			/// Draw text selecting rectangle to mark selected texts
			/// </summary>
			/// <param name="left">x coordinate of the top left point of the rectangle</param>
			/// <param name="top">y coordinate of the top left point of the rectangle</param>
			/// <param name="right">x coordinate of the bottom right point of the rectangle</param>
			/// <param name="bottom">y coordinate of the bottom right point of the rectangle</param>
			virtual void vpDrawSelRect(double left, double top, double right, double bottom)
			{
				if (!m_callback) return;
				RDRect rect;
				rect.left = left;
				rect.top = top;
				rect.right = right;
				rect.bottom = bottom;
				Windows::UI::Color clr;
				clr.A = 64;
				clr.B = 255;
				clr.G = 0;
				clr.R = 0;
				m_callback->cFillRect(rect, clr);
			}
			/// <summary>
			/// Draw a mark rectangle
			/// </summary>
			/// <param name="left">x coordinate of the top left point of the rectangle</param>
			/// <param name="top">y coordinate of the top left point of the rectangle</param>
			/// <param name="right">x coordinate of the bottom right point of the rectangle</param>
			/// <param name="bottom">y coordinate of the bottom right point of the rectangle</param>
			virtual void vpDrawMarkRect(double left, double top, double right, double bottom)
			{
				if (!m_callback) return;
				RDRect rect;
				rect.left = left;
				rect.top = top;
				rect.right = right;
				rect.bottom = bottom;
				Windows::UI::Color clr;
				clr.A = 64;
				clr.B = 0;
				clr.G = 0;
				clr.R = 0;
				m_callback->cFillRect(rect, clr);
			}
			virtual void vpShowBlock(Image^ img, double x, double y, double w, double h);
			virtual void vpRemoveBlock(Image^ img);
			virtual void vpShowPNO(TextBlock^ txt, double left, double top, double right, double bottom);
			virtual void vpRemovePNO(TextBlock^ txt);
			/// <summary>
			/// callback for text search mode. Invoked on text found or search reaches the end of page
			/// </summary>
			/// <param name="found"></param>
			virtual void vpOnFound(bool found)
			{
				if (found)
				{
					m_layout->vFindGoto();
					if (m_callback)
					{
						m_callback->cSetPos(m_layout->vGetX(), m_layout->vGetY());
						m_callback->cFound(found);
					}
				}
				else if (m_callback)
					m_callback->cFound(found);
			}
			/// <summary>
			/// Detach specified image
			/// </summary>
			/// <param name="bmp">Image to detach from the view</param>
			virtual void vpDetachBmp(WriteableBitmap^ bmp)
			{
				if (m_callback)
					m_callback->cDetachBmp(bmp);
			}
			/// <summary>
			/// Attach data to an image
			/// </summary>
			/// <param name="bmp">Image to receive the data</param>
			/// <param name="arr">Data will be written to image</param>
			virtual void vpAttachBmp(WriteableBitmap^ bmp, const Array<byte>^ arr)
			{
				if (m_callback)
					m_callback->cAttachBmp(bmp, arr);
			}
			virtual CoreDispatcher^ vpGetDisp()
			{
				return Dispatcher;
			}
		protected:
			Windows::Foundation::Size MeasureOverride(Windows::Foundation::Size size) override
			{
				Windows::Foundation::Size ret = Canvas::MeasureOverride(size);
				if (m_layout)
				{
					double mul_val = 1.0 / m_zoom;
					ret.Width = m_layout->vGetLayW() * mul_val;
					ret.Height = m_layout->vGetLayH() * mul_val;
				}
				else
				{
					ret.Width = size.Width;
					ret.Height = 0;
				}
				return ret;
			}
			Windows::Foundation::Size ArrangeOverride(Windows::Foundation::Size finalSize) override
			{
				Windows::Foundation::Size ret = Canvas::ArrangeOverride(finalSize);
				if (m_layout)
				{
					double mul_val = 1.0 / m_zoom;
					ret.Width = m_layout->vGetLayW() * mul_val;
					ret.Height = m_layout->vGetLayH() * mul_val;
				}
				else
				{
					ret.Width = finalSize.Width;
					ret.Height = 0;
				}
				return ret;
			}
		private:
			Rectangle^ m_bound;
			DOCXDoc^ m_doc;
			IContentListener^ m_callback;
			double m_zoom;
			CRDVLayout* m_layout;
			int m_page_sel;
		};
	}
}