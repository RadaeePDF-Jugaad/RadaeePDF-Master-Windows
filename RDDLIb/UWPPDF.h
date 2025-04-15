#ifndef _PDF_WINDOWS_RT_
#define _PDF_WINDOWS_RT_
#include "UWPCom.h"
#ifdef __cplusplus
extern "C" {
#endif

typedef struct _PDF_OUTLINE * PDF_OUTLINE;
typedef struct _PDF_PAGE *PDF_PAGE;
typedef struct _PDF_FINDER * PDF_FINDER;
typedef struct _PDF_ANNOT * PDF_ANNOT;
typedef struct _PDF_IMPORTCTX * PDF_IMPORTCTX;

typedef struct _PDF_PAGECONTENT * PDF_PAGECONTENT;
typedef struct _PDF_DOC_FONT * PDF_DOC_FONT;
typedef struct _PDF_PAGE_FONT * PDF_PAGE_FONT;
typedef struct _PDF_DOC_GSTATE * PDF_DOC_GSTATE;
typedef struct _PDF_PAGE_GSTATE * PDF_PAGE_GSTATE;
typedef struct _PDF_DOC_IMAGE * PDF_DOC_IMAGE;
typedef struct _PDF_PAGE_IMAGE * PDF_PAGE_IMAGE;
typedef struct _PDF_DOC_FORM * PDF_DOC_FORM;
typedef struct _PDF_PAGE_FORM * PDF_PAGE_FORM;
typedef struct _PDF_OBJ * PDF_OBJ;
typedef unsigned long long PDF_OBJ_REF;
typedef struct _PDF_HTML_EXPORTER* PDF_HTML_EXPORTER;
typedef struct _PDF_SIGN *PDF_SIGN;
typedef struct _PDF_TAG* PDF_TAG;
typedef struct _PDF_EDITNODE_* PDF_EDITNODE;

void PDF_Document_setOpenFlag(int flag);
PDF_DOC PDF_Document_openPath( const char *path, const char *password, PDF_ERR *err );
PDF_DOC PDF_Document_openPathW(const wchar_t* path, const char* password, PDF_ERR* err);
PDF_DOC PDF_Document_open( IRandomAccessStream ^stream, const char *password, PDF_ERR *err );
PDF_DOC PDF_Document_openStream( IPDFStream *stream, const char *password, PDF_ERR *err );
PDF_DOC PDF_Document_create( IRandomAccessStream ^stream, PDF_ERR *err );
PDF_DOC PDF_Document_createForStream( IPDFStream *stream, PDF_ERR *err );
PDF_DOC PDF_Document_createForPath( const char *path, PDF_ERR *err );
bool PDF_Document_setCache( PDF_DOC doc, const char *path );
String^ PDF_Document_getXMP(PDF_DOC doc);
bool PDF_Document_setXMP(PDF_DOC doc, String^ xmp);
bool PDF_Document_runJS( PDF_DOC doc, const char *js, IPDFJSDelegate *del);
PDF_HTML_EXPORTER PDF_Document_htmExpStart(PDF_DOC doc, String^ path);
bool PDF_Document_htmExpPage(PDF_DOC doc, PDF_HTML_EXPORTER exp, int pageno);
void PDF_Document_htmExpEnd(PDF_DOC doc, PDF_HTML_EXPORTER exp);
void PDF_Document_htmExpBinEnd(PDF_DOC doc, PDF_HTML_EXPORTER exp);
int PDF_Document_getPermission( PDF_DOC doc );
int PDF_Document_getPerm( PDF_DOC doc );
int PDF_Document_getEFCount(PDF_DOC doc);
String^ PDF_Document_getEFName(PDF_DOC doc, int index);
String^ PDF_Document_getEFDesc(PDF_DOC doc, int index);
bool PDF_Document_getEFData(PDF_DOC doc, int index, String^ path);
bool PDF_Document_delEF(PDF_DOC doc, int index);
bool PDF_Document_newEF(PDF_DOC doc, String^ path);
int PDF_Document_getJSCount(PDF_DOC doc);
String^ PDF_Document_getJSName(PDF_DOC doc, int index);
String^ PDF_Document_getJS(PDF_DOC doc, int index);
bool PDF_Document_exportForm(PDF_DOC doc, char* str, int len);
String^ PDF_Document_exportXFDF(PDF_DOC doc, String^ href);
bool PDF_Document_importXFDF(PDF_DOC doc, String^ xfdf);
bool PDF_Document_canSave( PDF_DOC doc );
PDF_TAG PDF_Document_newTagGroup(PDF_DOC doc, PDF_TAG parent, String^ tag);
int PDF_Document_getLinearizedStatus(PDF_DOC doc);

String ^PDF_Document_getOutlineLabel(PDF_DOC doc, PDF_OUTLINE outlinenode);
int PDF_Document_getOutlineDest( PDF_DOC doc, PDF_OUTLINE outlinenode );
void PDF_Document_getOutlineDest2(PDF_DOC doc, PDF_OUTLINE outlinenode, int* vals);
String^ PDF_Document_getOutlineFileLink(PDF_DOC doc, PDF_OUTLINE outlinenode);
PDF_OUTLINE PDF_Document_getOutlineChild(PDF_DOC doc, PDF_OUTLINE outlinenode);
PDF_OUTLINE PDF_Document_getOutlineNext(PDF_DOC doc, PDF_OUTLINE outlinenode);
bool PDF_Document_addOutlineChild(PDF_DOC doc, PDF_OUTLINE outlinenode, const wchar_t *label, int pageno, float top);
bool PDF_Document_addOutlineNext(PDF_DOC doc, PDF_OUTLINE outlinenode, const wchar_t *label, int pageno, float top);
bool PDF_Document_removeOutline(PDF_DOC doc, PDF_OUTLINE outlinenode);

String^ PDF_Document_getMeta(PDF_DOC doc, const char* tag);
bool PDF_Document_setMeta(PDF_DOC doc, const char* tag, const wchar_t* meta);
PDF_POINT PDF_Document_getPagesMaxSize(PDF_DOC doc);
float PDF_Document_getPageWidth( PDF_DOC doc, int pageno );
float PDF_Document_getPageHeight( PDF_DOC doc, int pageno );
int PDF_Document_getPageCount( PDF_DOC doc );
bool PDF_Document_getPageLabel(PDF_DOC doc, int pageno, wchar_t *wtxt, int wlen);
bool PDF_Document_setPageRotate(PDF_DOC doc, int pageno, int degree);
bool PDF_Document_changePageRect(PDF_DOC doc, int pageno, float dl, float dt, float dr, float db);
bool PDF_Document_save( PDF_DOC doc );
bool PDF_Document_saveAs( PDF_DOC doc, const char *dst );
bool PDF_Document_saveAsW(PDF_DOC doc, const wchar_t* dst);
bool PDF_Document_optimizeAsW(PDF_DOC doc, const wchar_t* dst, const unsigned char* opts, float img_dpi);
bool PDF_Document_encryptAsW(PDF_DOC doc, const wchar_t* dst, const wchar_t* upswd, const wchar_t* opswd, int perm, int method, const BYTE* id);
bool PDF_Document_isEncrypted( PDF_DOC doc );
int PDF_Document_verifySign(PDF_DOC doc, PDF_SIGN sign);
void PDF_Document_close( PDF_DOC doc );
PDF_PAGE PDF_Document_getPage( PDF_DOC doc, int pageno );
PDF_DOC_FONT PDF_Document_newFontCID( PDF_DOC doc, const char *name, int style );
float PDF_Document_getFontAscent( PDF_DOC doc, PDF_DOC_FONT font );
float PDF_Document_getFontDescent( PDF_DOC doc, PDF_DOC_FONT font );
PDF_DOC_GSTATE PDF_Document_newGState( PDF_DOC doc );
bool PDF_Document_setGStateStrokeAlpha( PDF_DOC doc, PDF_DOC_GSTATE state, int alpha );
bool PDF_Document_setGStateFillAlpha( PDF_DOC doc, PDF_DOC_GSTATE state, int alpha );
bool PDF_Document_setGStateStrokeDash(PDF_DOC doc, PDF_DOC_GSTATE state, const float *dash, int dash_cnt, float phase);
bool PDF_Document_setGStateBlendMode(PDF_DOC doc, PDF_DOC_GSTATE state, int bmode);

PDF_DOC_FORM PDF_Document_newForm(PDF_DOC doc);
PDF_PAGE_FONT PDF_Document_addFormResFont(PDF_DOC doc, PDF_DOC_FORM form, PDF_DOC_FONT font);
PDF_PAGE_IMAGE PDF_Document_addFormResImage(PDF_DOC doc, PDF_DOC_FORM form, PDF_DOC_IMAGE image);
PDF_PAGE_GSTATE PDF_Document_addFormResGState(PDF_DOC doc, PDF_DOC_FORM form, PDF_DOC_GSTATE gstate);
PDF_PAGE_FORM PDF_Document_addFormResForm(PDF_DOC doc, PDF_DOC_FORM form, PDF_DOC_FORM sub);
void PDF_Document_setFormContent(PDF_DOC doc, PDF_DOC_FORM form, float x, float y, float w, float h, PDF_PAGECONTENT content);
void PDF_Document_setFormTransparency(PDF_DOC doc, PDF_DOC_FORM form, bool isolate, bool knockout);
void PDF_Document_freeForm(PDF_DOC doc, PDF_DOC_FORM form);
void PDF_Document_setGenPDFA(PDF_DOC doc, bool gen);

PDF_PAGE PDF_Document_newPage( PDF_DOC doc, int pageno, float w, float h );
bool PDF_Document_removePage( PDF_DOC doc, int pageno );
PDF_IMPORTCTX PDF_Document_importStart( PDF_DOC doc, PDF_DOC doc_src );
bool PDF_Document_importPage( PDF_DOC doc, PDF_IMPORTCTX ctx, int srcno, int dstno );
bool PDF_Document_importPage2Page(PDF_DOC doc, PDF_IMPORTCTX ctx, int srcno, int dstno, const PDF_RECT* rect);
void PDF_Document_importEnd( PDF_DOC doc, PDF_IMPORTCTX ctx );
bool PDF_Document_movePage( PDF_DOC doc, int pageno1, int pageno2 );
PDF_DOC_IMAGE PDF_Document_newImage(PDF_DOC doc, WriteableBitmap ^bitmap, bool has_alpha, bool interpolate);
PDF_DOC_IMAGE PDF_Document_newImage2(PDF_DOC doc, SoftwareBitmap^ bitmap, bool has_alpha, bool interpolate);
PDF_DOC_IMAGE PDF_Document_newImageMatte(PDF_DOC doc, WriteableBitmap^ bitmap, unsigned int matte, bool interpolate);
PDF_DOC_IMAGE PDF_Document_newImage2Matte(PDF_DOC doc, SoftwareBitmap^ bitmap, unsigned int matte, bool interpolate);
PDF_DOC_IMAGE PDF_Document_newImageJPEG(PDF_DOC doc, const char *path, bool interpolate);
PDF_DOC_IMAGE PDF_Document_newImageJPX(PDF_DOC doc, const char *path, bool interpolate);

String ^PDF_Sign_getIssue(PDF_SIGN sign);
String ^ PDF_Sign_getSubject(PDF_SIGN sign);
long PDF_Sign_getVersion(PDF_SIGN sign);
String^ PDF_Sign_getName(PDF_SIGN sign);
String^ PDF_Sign_getLocation(PDF_SIGN sign);
String^ PDF_Sign_getReason(PDF_SIGN sign);
String^ PDF_Sign_getContact(PDF_SIGN sign);
String^ PDF_Sign_getModDT(PDF_SIGN sign);
Array<BYTE>^ PDF_Sign_getContent(PDF_SIGN sign);

int PDF_Page_sign(PDF_PAGE page, PDF_DOC_FORM appearence, const PDF_RECT *box, const char *cert_file, const char *pswd, const char *name, const char *reason, const char *location, const char *contact);
bool PDF_Page_getCropBox( PDF_PAGE page, PDF_RECT *box );
bool PDF_Page_getMediaBox( PDF_PAGE page, PDF_RECT *box );
void PDF_Page_close( PDF_PAGE page );

float PDF_Page_reflowStart(PDF_PAGE page, float width, float ratio, bool reflow_images);
bool PDF_Page_reflow(PDF_PAGE page, PDF_DIB dib, float orgx, float orgy);
int PDF_Page_reflowGetParaCount(PDF_PAGE page);
int PDF_Page_reflowGetCharCount(long page, int iparagraph);
float PDF_Page_reflowGetCharWidth(PDF_PAGE page, int iparagraph, int ichar);
float PDF_Page_reflowGetCharHeight(PDF_PAGE page, int iparagraph, int ichar);
int PDF_Page_reflowGetCharColor(PDF_PAGE page, int iparagraph, int ichar);
int PDF_Page_reflowGetCharUnicode(PDF_PAGE page, int iparagraph, int ichar);
PDF_RECT PDF_Page_reflowGetCharRect(PDF_PAGE page, int iparagraph, int ichar, int orgx, int orgy);
String ^PDF_Page_reflowGetText(PDF_PAGE page, int iparagraph1, int ichar1, int iparagraph2, int ichar2);
bool PDF_Page_reflowToBmp(PDF_PAGE page, PDF_BMP bitmap, float orgx, float orgy);

int PDF_Page_getPGEditorNodeCount(PDF_PAGE page);
void PDF_Page_setPGEditorModified(PDF_PAGE page, bool modified);
PDF_EDITNODE PDF_Page_getPGEditorNode1(PDF_PAGE page, int idx);
PDF_EDITNODE PDF_Page_getPGEditorNode2(PDF_PAGE page, float pdfx, float pdfy);
bool PDF_Page_renderWithPGEditor(PDF_PAGE page, PDF_DIB dib, PDF_MATRIX matrix, bool show_annots, int quality);
bool PDF_Page_updateWithPGEditor(PDF_PAGE page);
bool PDF_Page_cancelWithPGEditor(PDF_PAGE page);

void PDF_Page_renderPrepare( PDF_PAGE page, PDF_DIB dib );
bool PDF_Page_render( PDF_PAGE page, PDF_DIB dib, PDF_MATRIX matrix, bool show_annots, PDF_RENDER_MODE mode );
bool PDF_Page_renderToBmp( PDF_PAGE page, PDF_BMP bitmap, PDF_MATRIX matrix, bool show_annots, PDF_RENDER_MODE mode );
bool PDF_Page_renderAnnotToBmp(PDF_PAGE page, PDF_ANNOT annot, PDF_BMP bitmap);
void PDF_Page_renderCancel( PDF_PAGE page );
bool PDF_Page_renderIsFinished( PDF_PAGE page );
void PDF_Page_objsStart( PDF_PAGE page );
int PDF_Page_objsGetCharIndex( PDF_PAGE page, float x, float y );
int PDF_Page_objsGetCharCount( PDF_PAGE page );
int PDF_Page_objsGetString( PDF_PAGE page, int from, int to, char *buf, int len );
int PDF_Page_objsGetStringW( PDF_PAGE page, int from, int to, wchar_t *buf, int len );
void PDF_Page_objsGetCharRect( PDF_PAGE page, int index, PDF_RECT *rect );
const char *PDF_Page_objsGetCharFontName( PDF_PAGE page, int index );
int PDF_Page_objsAlignWord( PDF_PAGE page, int from, int dir );
bool PDF_Page_objsGetImageInfo(PDF_PAGE page, int index, int* info);
bool PDF_Page_objsSetImageJPEG(PDF_PAGE page, int index, String^ path, bool interpolate);
bool PDF_Page_objsSetImageJPX(PDF_PAGE page, int index, String^ path, bool interpolate);
bool PDF_Page_objsSetImageJPEGByMem(PDF_PAGE page, int index, const unsigned char* data, int len, bool interpolate);
bool PDF_Page_objsRemove(PDF_PAGE page, const int* ranges, int ranges_cnt, bool reload);

PDF_FINDER PDF_Page_findOpenW( PDF_PAGE page, const wchar_t *str, bool match_case, bool whole_word );
PDF_FINDER PDF_Page_findOpen2W(PDF_PAGE page, const wchar_t* str, bool match_case, bool whole_word, bool skip_blanks);
int PDF_Page_findGetCount( PDF_FINDER finder );
int PDF_Page_findGetFirstChar( PDF_FINDER finder, int index );
int PDF_Page_findGetEndChar(PDF_FINDER finder, int index);
void PDF_Page_findClose( PDF_FINDER finder );
int PDF_Page_getRotate(PDF_PAGE page);
int PDF_Page_getAnnotCount( PDF_PAGE page );
PDF_ANNOT PDF_Page_getAnnot( PDF_PAGE page, int index );
PDF_ANNOT PDF_Page_getAnnotFromPoint( PDF_PAGE page, float x, float y );
int PDF_Page_getAnnotSignStatus(PDF_PAGE page, PDF_ANNOT annot);
PDF_SIGN PDF_Page_getAnnotSign(PDF_PAGE page, PDF_ANNOT annot);
PDF_OBJ PDF_Page_getAnnotSignLock(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotSignLock(PDF_PAGE page, PDF_ANNOT annot, PDF_OBJ obj);

bool PDF_Page_isAnnotLocked( PDF_PAGE page, PDF_ANNOT annot );
void PDF_Page_setAnnotLock(PDF_PAGE page, PDF_ANNOT annot, bool lock);
bool PDF_Page_isAnnotLockedContent( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_isAnnotHide( PDF_PAGE page, PDF_ANNOT annot );
void PDF_Page_setAnnotHide( PDF_PAGE page, PDF_ANNOT annot, bool hide );

/**
	* get annotation type.<br/>
	* this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
	* this method valid in professional or premium version
	* @return type as these values:<br/>
	* 0:  unknown<br/>
	* 1:  text<br/>
	* 2:  link<br/>
	* 3:  free text<br/>
	* 4:  line<br/>
	* 5:  square<br/>
	* 6:  circle<br/>
	* 7:  polygon<br/>
	* 8:  polyline<br/>
	* 9:  text hilight<br/>
	* 10: text under line<br/>
	* 11: text squiggly<br/>
	* 12: text strikeout<br/>
	* 13: stamp<br/>
	* 14: caret<br/>
	* 15: ink<br/>
	* 16: popup<br/>
	* 17: file attachment<br/>
	* 18: sound<br/>
	* 19: movie<br/>
	* 20: widget<br/>
	* 21: screen<br/>
	* 22: print mark<br/>
	* 23: trap net<br/>
	* 24: water mark<br/>
	* 25: 3d object<br/>
	* 26: rich media
	*/
int PDF_Page_getAnnotType( PDF_PAGE page, PDF_ANNOT annot );
int PDF_Page_signAnnotField(PDF_PAGE page, PDF_ANNOT annot, PDF_DOC_FORM appearence, const char *cert_file, const char *pswd, const char* name, const char *reason, const char *location, const char *contact);

/**
	* get annotation field type in acroForm.<br/>
	* this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
	* this method valid in premium version
	* @return type as these values:<br/>
	* 0: unknown<br/>
	* 1: button field<br/>
	* 2: text field<br/>
	* 3: choice field<br/>
	* 4: signature field<br/>
	*/
int PDF_Page_getAnnotFieldType( PDF_PAGE page, PDF_ANNOT annot );
int PDF_Page_getAnnotFieldNameW( PDF_PAGE page, PDF_ANNOT annot, wchar_t *buf, int len );
int PDF_Page_getAnnotFieldNameWithNOW(PDF_PAGE page, PDF_ANNOT annot, wchar_t *buf, int len);
int PDF_Page_getAnnotFieldFullNameW( PDF_PAGE page, PDF_ANNOT annot, wchar_t *buf, int len );
int PDF_Page_getAnnotFieldFullName2W( PDF_PAGE page, PDF_ANNOT annot, wchar_t *buf, int len );
String ^PDF_Page_getAnnotFieldJS(PDF_PAGE page, PDF_ANNOT annot, int idx);
bool PDF_Page_isAnnotReadOnly(PDF_PAGE page, PDF_ANNOT annot);
void PDF_Page_setAnnotReadOnly(PDF_PAGE page, PDF_ANNOT annot, bool lock);
void PDF_Page_getAnnotRect( PDF_PAGE page, PDF_ANNOT annot, PDF_RECT *rect );
void PDF_Page_setAnnotRect( PDF_PAGE page, PDF_ANNOT annot, const PDF_RECT *rect );

PDF_PATH PDF_Page_getAnnotInkPath(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotInkPath(PDF_PAGE page, PDF_ANNOT annot, PDF_PATH path);
int PDF_Page_getAnnotFillColor( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_setAnnotFillColor( PDF_PAGE page, PDF_ANNOT annot, int color );
int PDF_Page_getAnnotStrokeColor( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_setAnnotStrokeColor( PDF_PAGE page, PDF_ANNOT annot, int color );
float PDF_Page_getAnnotStrokeWidth( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_setAnnotStrokeWidth( PDF_PAGE page, PDF_ANNOT annot, float width );
int PDF_Page_getAnnotStrokeDash(PDF_PAGE page, PDF_ANNOT annot, float* dash, int max);
bool PDF_Page_setAnnotStrokeDash(PDF_PAGE page, PDF_ANNOT annot, const float *dash, int cnt);
/**
	* set icon for sticky text note/file attachment annotation.<br/>
	* this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
	* you need render page again to show modified annotation.<br/>
	* this method valid in professional or premium version
	* @param icon icon value depends on annotation type.<br/>
	* <strong>For sticky text note:</strong><br/>
	* 0: Note<br/>
	* 1: Comment<br/>
	* 2: Key<br/>
	* 3: Help<br/>
	* 4: NewParagraph<br/>
	* 5: Paragraph<br/>
	* 6: Insert<br/>
	* 7: Check<br/>
	* 8: Circle<br/>
	* 9: Cross<br/>
	* <strong>For file attachment:</strong><br/>
	* 0: PushPin<br/>
	* 1: Graph<br/>
	* 2: Paperclip<br/>
	* 3: Tag<br/>
	* @return true or false.
	*/
bool PDF_Page_setAnnotIcon( PDF_PAGE page, PDF_ANNOT annot, int icon );
/**
	* get icon value for sticky text note/file attachment annotation.<br/>
	* this can be invoked after ObjsStart or Render or RenderToBmp.<br/>
	* this method valid in professional or premium version
	* @return icon value depends on annotation type.<br/>
	* <strong>For sticky text note:</strong><br/>
	* 0: Note<br/>
	* 1: Comment<br/>
	* 2: Key<br/>
	* 3: Help<br/>
	* 4: NewParagraph<br/>
	* 5: Paragraph<br/>
	* 6: Insert<br/>
	* 7: Check<br/>
	* 8: Circle<br/>
	* 9: Cross<br/>
	* <strong>For file attachment:</strong><br/>
	* 0: PushPin<br/>
	* 1: Graph<br/>
	* 2: Paperclip<br/>
	* 3: Tag<br/>
	*/
int PDF_Page_getAnnotIcon( PDF_PAGE page, PDF_ANNOT annot );

int PDF_Page_getAnnotDest( PDF_PAGE page, PDF_ANNOT annot );
String ^PDF_Page_getAnnotJS(PDF_PAGE page, PDF_ANNOT annot);
String ^PDF_Page_getAnnotURI(PDF_PAGE page, PDF_ANNOT annot);
String^ PDF_Page_getAnnot3D( PDF_PAGE page, PDF_ANNOT annot);
String^ PDF_Page_getAnnotMovie(PDF_PAGE page, PDF_ANNOT annot);
String^ PDF_Page_getAnnotSound(PDF_PAGE page, PDF_ANNOT annot);
String^ PDF_Page_getAnnotAttachment(PDF_PAGE page, PDF_ANNOT annot);
String^ PDF_Page_getAnnotRendition(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_getAnnot3DData( PDF_PAGE page, PDF_ANNOT annot, const char *path );
bool PDF_Page_getAnnotMovieData( PDF_PAGE page, PDF_ANNOT annot, const char *path );
bool PDF_Page_getAnnotSoundData( PDF_PAGE page, PDF_ANNOT annot, int *paras, const char *path );
bool PDF_Page_getAnnotAttachmentData( PDF_PAGE page, PDF_ANNOT annot, const char *path );
bool PDF_Page_getAnnotRenditionData(PDF_PAGE page, PDF_ANNOT annot, const char* path);
int PDF_Page_getAnnotRichMediaItemCount(PDF_PAGE page, PDF_ANNOT annot);
int PDF_Page_getAnnotRichMediaItemActived(PDF_PAGE page, PDF_ANNOT annot);
int PDF_Page_getAnnotRichMediaItemType(PDF_PAGE page, PDF_ANNOT annot, int idx);
String ^PDF_Page_getAnnotRichMediaItemAsset(PDF_PAGE page, PDF_ANNOT annot, int idx);
String ^PDF_Page_getAnnotRichMediaItemPara(PDF_PAGE page, PDF_ANNOT annot, int idx);
String ^PDF_Page_getAnnotRichMediaItemSource(PDF_PAGE page, PDF_ANNOT annot, int idx);
bool PDF_Page_getAnnotRichMediaItemSourceData(PDF_PAGE page, PDF_ANNOT annot, int idx, String ^save_path);
bool PDF_Page_getAnnotRichMediaData(PDF_PAGE page, PDF_ANNOT annot, String ^asset, String ^save_path);

String^ PDF_Page_getAnnotFileLink(PDF_PAGE page, PDF_ANNOT annot);
String^ PDF_Page_getAnnotRemoteDest(PDF_PAGE page, PDF_ANNOT annot);
PDF_ANNOT PDF_Page_getAnnotPopup(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_getAnnotPopupOpen(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotPopupOpen(PDF_PAGE page, PDF_ANNOT annot, bool open);
int PDF_Page_getAnnotReplyCount(PDF_PAGE page, PDF_ANNOT annot);
PDF_ANNOT PDF_Page_getAnnotReply(PDF_PAGE page, PDF_ANNOT annot, int idx);
String^ PDF_Page_getAnnotPopupSubject(PDF_PAGE page, PDF_ANNOT annot);
String^ PDF_Page_getAnnotPopupText(PDF_PAGE page, PDF_ANNOT annot);
String^ PDF_Page_getAnnotPopupLabel(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotPopupSubjectW( PDF_PAGE page, PDF_ANNOT annot, const wchar_t *subj );
bool PDF_Page_setAnnotPopupTextW( PDF_PAGE page, PDF_ANNOT annot, const wchar_t *text );
bool PDF_Page_setAnnotPopupLabelW(PDF_PAGE page, PDF_ANNOT annot, const wchar_t *text);
PDF_PATH PDF_Page_getAnnotPolygonPath(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotPolygonPath(PDF_PAGE page, PDF_ANNOT annot, PDF_PATH path);
PDF_PATH PDF_Page_getAnnotPolylinePath(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotPolylinePath(PDF_PAGE page, PDF_ANNOT annot, PDF_PATH path);
bool PDF_Page_getAnnotLinePoint(PDF_PAGE page, PDF_ANNOT annot, int idx, PDF_POINT* pt);
bool PDF_Page_setAnnotLinePoint(PDF_PAGE page, PDF_ANNOT annot, float x1, float y1, float x2, float y2);
int PDF_Page_getAnnotLineStyle(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotLineStyle(PDF_PAGE page, PDF_ANNOT annot, int style);

int PDF_Page_getAnnotEditType( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_getAnnotEditTextRect( PDF_PAGE page, PDF_ANNOT annot, PDF_RECT *rect );
float PDF_Page_getAnnotEditTextSize( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_setAnnotEditTextSize(PDF_PAGE page, PDF_ANNOT annot, float fsize);
int PDF_Page_getAnnotEditTextAlign(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotEditTextAlign(PDF_PAGE page, PDF_ANNOT annot, int align);

String^ PDF_Page_getAnnotEditText(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotEditFont(PDF_PAGE page, PDF_ANNOT annot, PDF_DOC_FONT font);
bool PDF_Page_setAnnotEditTextW( PDF_PAGE page, PDF_ANNOT annot, const wchar_t *text );
unsigned int PDF_Page_getAnnotEditTextColor(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_setAnnotEditTextColor(PDF_PAGE page, PDF_ANNOT annot, unsigned int color);
int PDF_Page_getAnnotComboItemCount( PDF_PAGE page, PDF_ANNOT annot );
String^ PDF_Page_getAnnotComboItem(PDF_PAGE page, PDF_ANNOT annot, int item);
int PDF_Page_getAnnotComboItemSel( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_setAnnotComboItem( PDF_PAGE page, PDF_ANNOT annot, int item );
int PDF_Page_getAnnotListItemCount( PDF_PAGE page, PDF_ANNOT annot );
String^ PDF_Page_getAnnotListItem(PDF_PAGE page, PDF_ANNOT annot, int item);
int PDF_Page_getAnnotListSels( PDF_PAGE page, PDF_ANNOT annot, int *sels, int sels_max );
bool PDF_Page_setAnnotListSels( PDF_PAGE page, PDF_ANNOT annot, const int *sels, int sels_cnt );
int PDF_Page_getAnnotCheckStatus( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_setAnnotCheckValue( PDF_PAGE page, PDF_ANNOT annot, bool check );
bool PDF_Page_setAnnotRadio( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_getAnnotReset( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_setAnnotReset( PDF_PAGE page, PDF_ANNOT annot );
String ^PDF_Page_getAnnotSubmitTarget( PDF_PAGE page, PDF_ANNOT annot );
bool PDF_Page_getAnnotSubmitParaW( PDF_PAGE page, PDF_ANNOT annot, wchar_t *para, int len );

bool PDF_Page_moveAnnot( PDF_PAGE PDF_Page_src, PDF_PAGE PDF_Page_dst, PDF_ANNOT annot, const PDF_RECT *rect );
bool PDF_Page_copyAnnot( PDF_PAGE page, PDF_ANNOT annot, const PDF_RECT *rect );
bool PDF_Page_removeAnnot( PDF_PAGE page, PDF_ANNOT annot );

bool PDF_Page_addFieldButton(PDF_PAGE page, const PDF_RECT* rect, String^ name, String^ label, PDF_DOC_FORM app);
bool PDF_Page_addFieldCheck(PDF_PAGE page, const PDF_RECT* rect, String^ name, String^ val, PDF_DOC_FORM app_on, PDF_DOC_FORM app_off);
bool PDF_Page_addFieldRadio(PDF_PAGE page, const PDF_RECT* rect, String^ name, String^ val, PDF_DOC_FORM app_on, PDF_DOC_FORM app_off);
bool PDF_Page_addFieldCombo(PDF_PAGE page, const PDF_RECT* rect, String^ name, const Array<String^>^ opts);
bool PDF_Page_addFieldList(PDF_PAGE page, const PDF_RECT* rect, String^ name, const Array<String^>^ opts, bool multi_sel);
bool PDF_Page_addFieldEditbox(PDF_PAGE page, const PDF_RECT* rect, String^ name, bool multi_line, bool password);
bool PDF_Page_addFieldSign(PDF_PAGE page, const PDF_RECT* rect, String^ name);

int PDF_Page_exportAnnot(PDF_PAGE page, PDF_ANNOT annot, unsigned char *data, int data_len);
bool PDF_Page_importAnnot(PDF_PAGE page, const PDF_RECT *rect, const unsigned char *data, int data_len);
PDF_OBJ_REF PDF_Page_getAnnotRef(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_addAnnot(PDF_PAGE page, PDF_OBJ_REF ref, int index);
bool PDF_Page_addAnnotPopup(PDF_PAGE page, PDF_ANNOT parent, const PDF_RECT *rect, bool open);
bool PDF_Page_addAnnotMarkup2(PDF_PAGE page, int ci1, int ci2, int color, int type);
bool PDF_Page_addAnnotMarkup( PDF_PAGE page, PDF_MATRIX matrix, const PDF_RECT *rects, int rects_cnt, int color, int type );
bool PDF_Page_addAnnotGoto( PDF_PAGE page, PDF_MATRIX matrix, const PDF_RECT *rect, int pageno, float top );
bool PDF_Page_addAnnotGoto2( PDF_PAGE page, const PDF_RECT *rect, int pageno, float top );
bool PDF_Page_addAnnotUri( PDF_PAGE page, PDF_MATRIX matrix, const PDF_RECT *rect, const char *uri );
bool PDF_Page_addAnnotURI2( PDF_PAGE page, const PDF_RECT *rect, const char *uri );
bool PDF_Page_addAnnotInk( PDF_PAGE page, PDF_MATRIX matrix, PDF_INK hand, float orgx, float orgy );
bool PDF_Page_addAnnotInk2( PDF_PAGE page, PDF_INK hand );
bool PDF_Page_addAnnotPolygon(PDF_PAGE page, PDF_PATH hand, int color, int fill_color, float width);
bool PDF_Page_addAnnotPolyline(PDF_PAGE page, PDF_PATH hand, int style1, int style2, int color, int fill_color, float width);
bool PDF_Page_addAnnotLine( PDF_PAGE page, PDF_MATRIX matrix, const PDF_POINT *pt1, const PDF_POINT *pt2, int style1, int style2, float width, int color, int icolor );
bool PDF_Page_addAnnotLine2( PDF_PAGE page, const PDF_POINT *pt1, const PDF_POINT *pt2, int style1, int style2, float width, int color, int icolor );
bool PDF_Page_addAnnotRect( PDF_PAGE page, PDF_MATRIX matrix, const PDF_RECT *rect, float width, int color, int icolor );
bool PDF_Page_addAnnotRect2( PDF_PAGE page, const PDF_RECT *rect, float width, int color, int icolor );
bool PDF_Page_addAnnotEllipse( PDF_PAGE page, PDF_MATRIX matrix, const PDF_RECT *rect, float width, int color, int icolor );
bool PDF_Page_addAnnotEllipse2( PDF_PAGE page, const PDF_RECT *rect, float width, int color, int icolor );
bool PDF_Page_addAnnotText( PDF_PAGE page, PDF_MATRIX matrix, float x, float y );
bool PDF_Page_addAnnotText2( PDF_PAGE page, float x, float y );
bool PDF_Page_addAnnotEditbox( PDF_PAGE page, PDF_MATRIX matrix, const PDF_RECT *rect, int line_clr, float line_w, int fill_clr, float tsize, int text_clr );
bool PDF_Page_addAnnotEditbox2( PDF_PAGE page, const PDF_RECT *rect, int line_clr, float line_w, int fill_clr, float tsize, int text_clr );
bool PDF_Page_addAnnotBitmap( PDF_PAGE page, PDF_MATRIX matrix, PDF_DOC_IMAGE img, const PDF_RECT *rect );
bool PDF_Page_addAnnotBitmap2( PDF_PAGE page, PDF_DOC_IMAGE img, const PDF_RECT *rect );
bool PDF_Page_addAnnotAttachment( PDF_PAGE page, const char *path, int icon, const PDF_RECT *rect );
bool PDF_Page_addAnnotRichMedia(PDF_PAGE page, String ^path_player, String ^path_content, int type, PDF_DOC_IMAGE dimage, const PDF_RECT *rect);


PDF_PAGECONTENT PDF_PageContent_create();
void PDF_PageContent_gsSave( PDF_PAGECONTENT content );
void PDF_PageContent_gsRestore( PDF_PAGECONTENT content );
void PDF_PageContent_gsSet( PDF_PAGECONTENT content, PDF_PAGE_GSTATE gs );
void PDF_PageContent_gsSetMatrix( PDF_PAGECONTENT content, PDF_MATRIX mat );
void PDF_PageContent_textBegin( PDF_PAGECONTENT content );
void PDF_PageContent_textEnd( PDF_PAGECONTENT content );
void PDF_PageContent_drawImage( PDF_PAGECONTENT content, PDF_PAGE_IMAGE img );
void PDF_PageContent_drawForm(PDF_PAGECONTENT content, PDF_PAGE_FORM form);
PDF_POINT PDF_PageContent_textGetSizeW(PDF_PAGECONTENT content, PDF_PAGE_FONT font, const wchar_t *text, float width, float height, float char_space, float word_space);
void PDF_PageContent_drawTextW( PDF_PAGECONTENT content, const wchar_t *text );
int PDF_PageContent_drawText2W(PDF_PAGECONTENT content, const wchar_t* text, int align, float width);
int PDF_PageContent_drawText3W(PDF_PAGECONTENT content, const wchar_t* text, int align, float width, int max_lines);
void PDF_PageContent_strokePath( PDF_PAGECONTENT content, PDF_PATH path );
void PDF_PageContent_fillPath( PDF_PAGECONTENT content, PDF_PATH path, bool winding );
void PDF_PageContent_clipPath( PDF_PAGECONTENT content, PDF_PATH path, bool winding );
void PDF_PageContent_setFillColor( PDF_PAGECONTENT content, int color );
void PDF_PageContent_setStrokeColor( PDF_PAGECONTENT content, int color );
void PDF_PageContent_setStrokeCap( PDF_PAGECONTENT content, int cap );
void PDF_PageContent_setStrokeJoin( PDF_PAGECONTENT content, int join );
void PDF_PageContent_setStrokeWidth( PDF_PAGECONTENT content, float w );
void PDF_PageContent_setStrokeMiter( PDF_PAGECONTENT content, float miter );
void PDF_PageContent_setStrokeDash(PDF_PAGECONTENT content, const float* dash, int dash_cnt, float phase);
void PDF_PageContent_textSetCharSpace( PDF_PAGECONTENT content, float space );
void PDF_PageContent_textSetWordSpace( PDF_PAGECONTENT content, float space );
void PDF_PageContent_textSetLeading( PDF_PAGECONTENT content, float leading );
void PDF_PageContent_textSetRise( PDF_PAGECONTENT content, float rise );
void PDF_PageContent_textSetHScale( PDF_PAGECONTENT content, int scale );
void PDF_PageContent_textNextLine( PDF_PAGECONTENT content );
void PDF_PageContent_textMove( PDF_PAGECONTENT content, float x, float y );
void PDF_PageContent_textSetFont( PDF_PAGECONTENT content, PDF_PAGE_FONT font, float size );
void PDF_PageContent_textSetRenderMode( PDF_PAGECONTENT content, int mode );
void PDF_PageContent_destroy( PDF_PAGECONTENT content );
void PDF_PageContent_tagBlockStart(PDF_PAGECONTENT content, PDF_TAG block);
void PDF_PageContent_tagBlockEnd(PDF_PAGECONTENT content);

PDF_TAG PDF_Page_newTagBlock(PDF_PAGE page, PDF_TAG parent, String^ tag);
PDF_PAGE_FONT PDF_Page_addResFont( PDF_PAGE page, PDF_DOC_FONT font );
PDF_PAGE_IMAGE PDF_Page_addResImage( PDF_PAGE page, PDF_DOC_IMAGE image );
PDF_PAGE_GSTATE PDF_Page_addResGState( PDF_PAGE page, PDF_DOC_GSTATE gstate );
PDF_PAGE_FORM PDF_Page_addResForm(PDF_PAGE page, PDF_DOC_FORM form);
bool PDF_Page_flate(PDF_PAGE page);
bool PDF_Page_flateAnnot(PDF_PAGE page, PDF_ANNOT annot);
bool PDF_Page_addContent( PDF_PAGE page, PDF_PAGECONTENT content, bool flush );

void PDF_EditNode_destroy(PDF_EDITNODE enode);
void PDF_EditNode_delete(PDF_EDITNODE enode);
int PDF_EditNode_getType(PDF_EDITNODE enode);
void PDF_EditNode_updateRect(PDF_EDITNODE enode);
bool PDF_EditNode_getRect(PDF_EDITNODE enode, PDF_RECT* ret);
void PDF_EditNode_setRect(PDF_EDITNODE enode, const PDF_RECT* rect);
long long PDF_EditNode_getCharPos(PDF_EDITNODE enode, float pdfx, float pdfy);
long long PDF_EditNode_getCharPrev(PDF_EDITNODE enode, long long pos);
long long PDF_EditNode_getCharNext(PDF_EDITNODE enode, long long pos);
long long PDF_EditNode_getCharPrevLine(PDF_EDITNODE enode, float y, long long pos);
long long PDF_EditNode_getCharNextLine(PDF_EDITNODE enode, float y, long long pos);
long long PDF_EditNode_getCharBegLine(PDF_EDITNODE enode, long long pos);
long long PDF_EditNode_getCharEndLine(PDF_EDITNODE enode, long long pos);
bool PDF_EditNode_getCharRect(PDF_EDITNODE enode, long long pos, PDF_RECT* ret);
void PDF_EditNode_charDelete(PDF_EDITNODE enode, long long start, long long end);
long long PDF_EditNode_charInsert(PDF_EDITNODE enode, long long pos, String ^val);
String^ PDF_EditNode_charGetString(PDF_EDITNODE enode, long long pos0, long long pos1);
void PDF_EditNode_charReturn(PDF_EDITNODE enode, long long pos);
void PDF_EditNode_setDefFont(String^ fname);
void PDF_EditNode_setDefCJKFont(String^ fname);

PDF_OBJ PDF_Obj_create();
void PDF_Obj_destroy(PDF_OBJ hand);
int PDF_Obj_dictGetItemCount(PDF_OBJ hand);
const char *PDF_Obj_dictGetItemName(PDF_OBJ hand, int index);
PDF_OBJ PDF_Obj_dictGetItemByIndex(PDF_OBJ hand, int index);
PDF_OBJ PDF_Obj_dictGetItemByName(PDF_OBJ hand, const char *name);
void PDF_Obj_dictSetItem(PDF_OBJ hand, const char *name);
void PDF_Obj_dictRemoveItem(PDF_OBJ hand, const char *name);
int PDF_Obj_arrayGetItemCount(PDF_OBJ hand);
PDF_OBJ PDF_Obj_arrayGetItem(PDF_OBJ hand, int index);
void PDF_Obj_arrayAppendItem(PDF_OBJ hand);
void PDF_Obj_arrayInsertItem(PDF_OBJ hand, int index);
void PDF_Obj_arrayRemoveItem(PDF_OBJ hand, int index);
void PDF_Obj_arrayClear(PDF_OBJ hand);
bool PDF_Obj_getBoolean(PDF_OBJ hand);
void PDF_Obj_setBoolean(PDF_OBJ hand, bool v);
int PDF_Obj_getInt(PDF_OBJ hand);
void PDF_Obj_setInt(PDF_OBJ hand, int v);
float PDF_Obj_getReal(PDF_OBJ hand);
void PDF_Obj_setReal(PDF_OBJ hand, float v);
const char *PDF_Obj_getName(PDF_OBJ hand);
void PDF_Obj_setName(PDF_OBJ hand, const char *v);
const char *PDF_Obj_getAsciiString(PDF_OBJ hand);
String ^PDF_Obj_getTextString(PDF_OBJ hand);
unsigned char *PDF_Obj_getHexString(PDF_OBJ hand, int *len);
void PDF_Obj_setAsciiString(PDF_OBJ hand, const char *v);
void PDF_Obj_setTextString(PDF_OBJ hand, const wchar_t *v);
void PDF_Obj_setHexString(PDF_OBJ hand, unsigned char *v, int len);
PDF_OBJ_REF PDF_Obj_getReference(PDF_OBJ hand);
void PDF_Obj_setReference(PDF_OBJ hand, PDF_OBJ_REF v);
int PDF_Obj_getType(PDF_OBJ hand);
PDF_OBJ PDF_Document_advGetObj(PDF_DOC doc, PDF_OBJ_REF ref);
PDF_OBJ_REF PDF_Document_advNewIndirectObj(PDF_DOC doc);
PDF_OBJ_REF PDF_Document_advNewIndirectObjWithData(PDF_DOC doc, PDF_OBJ PDF_Obj_hand);
PDF_OBJ_REF PDF_Document_advGetRef(PDF_DOC doc);
void PDF_Document_advReload(PDF_DOC doc);
PDF_OBJ_REF PDF_Document_advNewFlateStream(PDF_DOC doc, const unsigned char *source, int len);
PDF_OBJ_REF PDF_Document_advNewRawStream(PDF_DOC doc, const unsigned char *source, int len);
PDF_OBJ_REF PDF_Page_advGetAnnotRef(PDF_PAGE page, PDF_ANNOT annot);
PDF_OBJ_REF PDF_Page_advGetRef(PDF_PAGE page);
void PDF_Page_advReloadAnnot(PDF_PAGE page, PDF_ANNOT annot);
void PDF_Page_advReload(PDF_PAGE page);

#ifdef __cplusplus
}
#endif

#endif
