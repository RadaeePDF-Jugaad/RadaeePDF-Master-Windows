#pragma once
#include "UWPDOCX.h"
#include "RDCom.h"
#include "RDPDF.h"

using namespace RDDLib::comm;

namespace RDDLib
{
	namespace docx
	{
		ref class DOCXPage;
		ref class DOCXDoc;
		public ref class DOCXFinder sealed
		{
		public:
			/// <summary>
			/// Get find count in current page.
			/// </summary>
			/// <returns>Count or 0 if no found.</returns>
			int GetCount()
			{
				return DOCX_Page_findGetCount(m_finder);
			}
			/// <summary>
			/// Get first char index.
			/// </summary>
			/// <param name="index">0 based index value. range:[0, FindGetCount()-1]</param>
			/// <returns>The first char index of texts, see: ObjsGetString. range:[0, ObjsGetCharCount()-1]</returns>
			int GetFirstChar(int index)
			{
				return DOCX_Page_findGetFirstChar(m_finder, index);
			}
			/// <summary>
			/// Get last char index.
			/// </summary>
			/// <param name="index">0 based index value. range:[0, FindGetCount()-1]</param>
			/// <returns>The last char index of texts, see: ObjsGetString. range:[0, ObjsGetCharCount()-1]</returns>
			int GetLastChar(int index)
			{
				return DOCX_Page_findGetEndChar(m_finder, index);
			}
		private:
			DOCXFinder()
			{
				m_finder = 0;
			}
			~DOCXFinder()
			{
				DOCX_Page_findClose(m_finder);
			}
			friend DOCXPage;
			DOCX_FINDER m_finder;
		};
		public ref class DOCXPage sealed
		{
		public:
			/**
			 * prepare to render. it reset dib pixels to white value, and reset page status.<br/>
			 * if dib is null, only to reset page status.
			 * @param dib DIB object to render. get from Global.dibGet() or null.
			 */
			void RenderPrepare(RDDIB^ dib)
			{
				if (dib)
					DOCX_Page_renderPrepare(m_page, dib->m_dib);
				else
					DOCX_Page_renderPrepare(m_page, NULL);
			}
			/// <summary>
			/// Prepare to render.
			/// </summary>
			void RenderPrepare()
			{
				DOCX_Page_renderPrepare(m_page, NULL);
			}
			/// <summary>
			/// Render page to RDDIB object. this function returned for cancelled or finished.
			/// Before render, RenderPrepare() method must be invoked.
			/// </summary>
			/// <param name="dib">RDDIB object to render to</param>
			/// <param name="scale">Scale size which will be applied for rendering</param>
			/// <param name="orgx">Original x coordinate to start rendering</param>
			/// <param name="orgy">Original y coordinate to start rendering</param>
			/// <param name="quality">Render quality. Available values:
			/// 0: poor quality
			/// 1: normal quality
			/// 2: best quality</param>
			/// <returns>True if successed, otherwise false</returns>
			Boolean Render(RDDIB^ dib, float scale, int orgx, int orgy, int quality)
			{
				return DOCX_Page_render(m_page, dib->m_dib, scale, orgx, orgy, quality);
			}
			/**
			 * set page status to cancelled and cancel render function.
			 */
			void RenderCancel()
			{
				DOCX_Page_renderCancel(m_page);
			}
			/**
			 * check if page rendering is finished.
			 * @return true or false
			 */
			Boolean RenderIsFinished()
			{
				return DOCX_Page_renderIsFinished(m_page);
			}
			/**
			 * get text objects to memory.<br/>
			 * a standard license is required for this method
			 */
			void ObjsStart()
			{
				DOCX_Page_objsStart(m_page);
			}
			/**
			 * get chars count in this page. this can be invoked after ObjsStart<br/>
			 * a standard license is required for this method
			 * @return count or 0 if ObjsStart not invoked.
			 */
			int ObjsGetCharCount()
			{
				return DOCX_Page_objsGetCharCount(m_page);
			}
			/**
			 * get char's box in PDF coordinate system, this can be invoked after ObjsStart
			 * @param index 0 based unicode index.
			 * @param vals return 4 elements for PDF rectangle.
			 */
			RDRect ObjsGetCharRect(int index)
			{
				RDRect rect;
				DOCX_Page_objsGetCharRect(m_page, index, (PDF_RECT*)&rect);
				return rect;
			}
			/**
			 * get char index nearest to point
			 * @param pt point as [x,y] in PDF coordinate.
			 * @return char index or -1 failed.
			 */
			int ObjsGetCharIndex(float x, float y)
			{
				PDF_POINT pt;
				pt.x = x;
				pt.y = y;
				return DOCX_Page_objsGetCharIndex(m_page, &pt);
			}
			/**
			 * get index aligned by word. this can be invoked after ObjsStart
			 * @param from 0 based unicode index.
			 * @param dir if dir < 0,  get start index of the word. otherwise get last index of the word.
			 * @return new index value.
			 */
			int ObjsAlignWord(int index, int dir)
			{
				return DOCX_Page_objsAlignWord(m_page, index, dir);
			}
			/**
			 * get string from range. this can be invoked after ObjsStart
			 * @param from 0 based unicode index.
			 * @param to 0 based unicode index.
			 * @return string or null.
			 */
			String^ ObjsGetString(int from, int to)
			{
				return DOCX_Page_objsGetString(m_page, from, to);
			}
			String^ GetHLink(float x, float y)
			{
				return DOCX_Page_getHLink(m_page, x, y);
			}
			/**
			 * create a find session. this can be invoked after ObjsStart
			 * @param str key string to find.
			 * @param match_case match case?
			 * @param whole_word match whole word?
			 * @return handle of find session, or 0 if no found.
			 */
			DOCXFinder^ GetFinder(String^ key, Boolean match_case, Boolean whole_word)
			{
				DOCX_FINDER find = DOCX_Page_findOpen(m_page, key, match_case, whole_word);
				if (find)
				{
					DOCXFinder^ finder = ref new DOCXFinder();
					finder->m_finder = find;
					return finder;
				}
				else return nullptr;
			}
			/**
			 * create a find session. this can be invoked after ObjsStart<br/>
			 * this function treats line break as blank char.
			 * @param str key string to find.
			 * @param match_case match case?
			 * @param whole_word match whole word?
			 * @param skip_blank skip blank?
			 * @return handle of find session, or 0 if no found.
			 */
			DOCXFinder^ GetFinder(String^ key, Boolean match_case, Boolean whole_word, Boolean skip_blanks)
			{
				DOCX_FINDER find = DOCX_Page_findOpen2(m_page, key, match_case, whole_word, skip_blanks);
				if (find)
				{
					DOCXFinder^ finder = ref new DOCXFinder();
					finder->m_finder = find;
					return finder;
				}
				else return nullptr;
			}
			/// <summary>
			/// Close page object and free memory.
			/// </summary>
			void Close()
			{
				if (!m_ref && m_page)
					DOCX_Page_close(m_page);
				m_page = NULL;
				m_ref = false;
			}
		private:
			DOCXPage()
			{
				//m_doc = nullptr;
				m_page = NULL;
				m_ref = false;
			}
			~DOCXPage()
			{
				Close();
			}
			friend DOCXDoc;
			bool m_ref;
			//DOCXDoc ^m_doc;
			DOCX_PAGE m_page;
		};
		public ref class DOCXDoc sealed
		{
		public:
			DOCXDoc()
			{
				m_doc = NULL;
				m_inner = NULL;
			}
			/**
			 * open document.<br/>
			 * first time, SDK try password as user password, and then try password as owner password.
			 * @param stream IRandomAccessStream object.
			 * @param password password or null.
			 * @return error code:<br/>
			 * 0:succeeded, and continue<br/>
			 * -1:need input password<br/>
			 * -2:unknown encryption<br/>
			 * -3:damaged or invalid format<br/>
			 * -10:access denied or invalid file path<br/>
			 * others:unknown error
			 */
			RD_ERROR Open(IRandomAccessStream^ stream, String^ password)
			{
				PDF_ERR err;
				m_doc = DOCX_Document_open(stream, password, &err);
				if (m_doc) return RD_ERROR::err_ok;
				else return (RD_ERROR)err;
			}
			/// <summary>
			/// Open a document in stream mode
			/// </summary>
			/// <param name="stream">A IRDStream which provides the document content</param>
			/// <param name="password">Password if the document is a password protected document</param>
			/// <returns>A RD_ERROR which indicate the result of opening the document. Please refer to definition of RD_ERROR class for more details</returns>
			RD_ERROR OpenStream(IRDStream^ stream, String^ password)
			{
				if (!stream) return RD_ERROR::err_invalid_para;
				PDF_ERR err;
				m_inner = new DOCXStreamInner;
				m_inner->Open(stream);
				m_doc = DOCX_Document_openStream(m_inner, password, &err);
				if (m_doc) return RD_ERROR::err_ok;
				else return (RD_ERROR)err;
			}
			/**
			 * open document.<br/>
			 * first time, SDK try password as user password, and then try password as owner password.
			 * @param path PDF file to be open.
			 * @param password password or null.
			 * @return error code:<br/>
			 * 0:succeeded, and continue<br/>
			 * -1:need input password<br/>
			 * -2:unknown encryption<br/>
			 * -3:damaged or invalid format<br/>
			 * -10:access denied or invalid file path<br/>
			 * others:unknown error
			 */
			RD_ERROR OpenPath(String^ path, String ^password)
			{
				PDF_ERR err;
				m_doc = DOCX_Document_openPath(path, password, &err);
				if (m_doc) return RD_ERROR::err_ok;
				else return (RD_ERROR)err;
			}
			/// <summary>
			/// Close opened document
			/// </summary>
			void Close()
			{
				DOCX_Document_close(m_doc);
				m_doc = NULL;
				if (m_inner)
				{
					m_inner->Close();
					delete m_inner;
				}
				m_inner = NULL;
			}
			/**
			 * max width and max height of all pages.
			 */
			property RDPoint MaxPageSize
			{
				RDPoint get()
				{
					PDF_POINT pt;
					DOCX_Document_getPagesMaxSize(m_doc, &pt);
					return *(RDPoint*)&pt;
				}
			}
			/**
			 * get page width by page NO.
			 * @param pageno 0 based page NO. range:[0, GetPageCount()-1]
			 * @return width value.
			 */
			float GetPageWidth(int pageno)
			{
				return DOCX_Document_getPageWidth(m_doc, pageno);
			}
			/**
			 * get page height by page NO.
			 * @param pageno 0 based page NO. range:[0, GetPageCount()-1]
			 * @return height value.
			 */
			float GetPageHeight(int pageno)
			{
				return DOCX_Document_getPageHeight(m_doc, pageno);
			}
			property unsigned long long Handler
			{
				unsigned long long get() { return (unsigned long long)m_doc; }
			}
			/// <summary>
			/// Get page count of the document
			/// </summary>
			property int PageCount
			{
				int get() { return DOCX_Document_getPageCount(m_doc); }
			}
			/// <summary>
			/// Get a DOCXPage object with specified page number
			/// </summary>
			/// <param name="pageno">Page number of target page</param>
			/// <returns>DOCXPage object of the docx page</returns>
			DOCXPage^ GetPage(int pageno)
			{
				DOCX_PAGE page = DOCX_Document_getPage(m_doc, pageno);
				if (page)
				{
					DOCXPage^ pg = ref new DOCXPage();
					pg->m_page = page;
					//pg->m_doc = this;
					return pg;
				}
				else
					return nullptr;
			}
			/// <summary>
			/// Get if the file is opened
			/// </summary>
			property Boolean IsOpened
			{
				Boolean get() { return m_doc != NULL; }
			}
			/**
			 * Export docx file to PDF file.<br/>
			 * this function require premium license. and can only export 3 pages if demo package actived.<br/>
			 * docx pages will append to tail of PDF file, if PDF file is not empty.<br/>
			 * export to no premium license or readonly PDF Object will return false.
			 * @param doc PDF document object.
			 * @return true or false.
			 */
			bool ExportPDF(pdf::PDFDoc^ pdf)
			{
				return DOCX_Document_exportPDF(m_doc, pdf->m_doc);
			}
		private:
			class DOCXStreamInner :public IPDFStream
			{
			public:
				void Open(IRDStream^ stream)
				{
					m_stream = stream;
				}
				virtual bool Writeable() const
				{
					return m_stream->Writeable();
				}
				virtual unsigned long long GetLen() const
				{
					return m_stream->GetLength();
				}
				virtual unsigned long long GetPos() const
				{
					return m_stream->GetPosition();
				}
				virtual bool SetPos(unsigned long long off)
				{
					return m_stream->SetPosition(off);
				}
				virtual unsigned int Read(void* pBuf, unsigned int dwBuf)
				{
					ArrayReference<BYTE> tmp((BYTE*)pBuf, dwBuf);
					return m_stream->Read(tmp);
				}
				virtual unsigned int Write(const void* pBuf, unsigned int dwBuf)
				{
					ArrayReference<BYTE> tmp((BYTE*)pBuf, dwBuf);
					return m_stream->Write(tmp);
				}
				virtual void Close()
				{
					m_stream->Close();
					m_stream = nullptr;
				}
				virtual void Flush()
				{
					m_stream->Flush();
				}
			protected:
				IRDStream^ m_stream;
			};
			~DOCXDoc()
			{
				Close();
			}
			DOCX_DOC m_doc;
			DOCXStreamInner* m_inner;
		};
	}
}