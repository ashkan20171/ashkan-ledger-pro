using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Globalization;
using System.Runtime.CompilerServices;
namespace AshkanLedgerPro;
public static class UiTranslator
{
 private static readonly Dictionary<string,string> Fa=new(StringComparer.OrdinalIgnoreCase)
 {
  ["Sales Invoices"]="فاکتورهای فروش",
  ["Purchase Invoices"]="فاکتورهای خرید",
  ["Receipts & Payments"]="دریافت‌ها و پرداخت‌ها",
  ["Check Register"]="دفتر چک‌ها",
  ["Bank Reconciliation"]="مغایرت‌گیری بانکی",
  ["User Management"]="مدیریت کاربران",
  ["Backup Database"]="پشتیبان‌گیری پایگاه داده",
  ["Export Journal CSV"]="خروجی CSV روزنامه",
  ["Overview"]="نمای کلی",
  ["Journal Entries"]="اسناد روزنامه",
  ["Inventory Catalog"]="فهرست کالاها",
  ["Stock Movements"]="گردش انبار",
  ["Profit & Loss"]="سود و زیان",
  ["Customers & Suppliers"]="مشتریان و تأمین‌کنندگان",
  ["New user"]="کاربر جدید",
  ["Create user"]="ایجاد کاربر",
  ["Login"]="ورود",
  ["Sign in"]="ورود به سیستم",
  ["Logout"]="خروج",
  ["Admin"]="مدیر سیستم",
  ["Accountant"]="حسابدار",
  ["Viewer"]="مشاهده‌گر",
  ["Error"]="خطا",
  ["Success"]="موفقیت",
  ["Warning"]="هشدار",
  ["Information"]="اطلاعات",
  ["Yes"]="بله",
  ["No"]="خیر",
  ["OK"]="تأیید",
  ["Add"]="افزودن",
  ["Edit"]="ویرایش",
  ["Refresh"]="به‌روزرسانی",
  ["Print"]="چاپ",
  ["Export"]="خروجی",
  ["History"]="تاریخچه",
  ["Details"]="جزئیات",
  ["Select"]="انتخاب",
  ["Date and time"]="تاریخ و زمان",
  ["Bank only"]="فقط بانک",
  ["System only"]="فقط سیستم",
  ["No records found"]="موردی یافت نشد",
  ["No data"]="داده‌ای موجود نیست",
  ["User"]="کاربر",
  ["All"]="همه",
  ["Today"]="امروز",
  ["From date"]="از تاریخ",
  ["To date"]="تا تاریخ",
  ["Currency"]="واحد پول",
  ["Toman"]="تومان",
  ["Rial"]="ریال",
  ["USD"]="دلار آمریکا",
  ["EUR"]="یورو",
  ["Check"]="چک",
  ["Due date"]="تاریخ سررسید",
  ["Amount paid"]="مبلغ پرداخت‌شده",
  ["Outstanding"]="تسویه‌نشده",
  ["Save changes"]="ذخیره تغییرات",
  ["New invoice"]="فاکتور جدید",
  ["Add line"]="افزودن ردیف",
  ["Remove line"]="حذف ردیف",
  ["Invoice number"]="شماره فاکتور",
  ["Unit cost"]="بهای واحد",
  ["Stock"]="موجودی",
  ["Current balance"]="مانده فعلی",
  ["Transaction date"]="تاریخ تراکنش",
  ["Select customer"]="انتخاب مشتری",
  ["Select supplier"]="انتخاب تأمین‌کننده",
  ["Invalid credentials."]="نام کاربری یا رمز عبور نامعتبر است.",

  ["●  Finance Workspace"]="●  محیط مالی",
  ["Select both accounts."]="هر دو حساب را انتخاب کنید.",
  ["Enter a valid amount."]="مبلغ معتبر وارد کنید.",
  ["Invalid amount."]="مبلغ نامعتبر است.",
  ["Validation error"]="خطای اعتبارسنجی",
  ["Export journal"]="خروجی اسناد روزنامه",
  ["CSV exported successfully."]="فایل CSV با موفقیت ذخیره شد.",
  ["Export complete"]="خروجی آماده شد",
  ["Export failed"]="خطا در خروجی",
  ["Select a product."]="یک کالا انتخاب کنید.",
  ["Quantity must be positive."]="تعداد باید مثبت باشد.",
  ["Invalid unit cost."]="بهای واحد نامعتبر است.",
  ["Purchase"]="خرید",
  ["Sales"]="فروش",
  ["Bank reconciliation preview"]="پیش‌نمایش مغایرت‌گیری بانکی",
  ["Import bank CSV and compare"]="ورود CSV بانک و مقایسه",
  ["Save comparison snapshot"]="ذخیره نتیجه مغایرت‌گیری",
  ["Previous comparison snapshots"]="سوابق مغایرت‌گیری",
  ["View selected snapshot details"]="نمایش جزئیات مغایرت‌گیری انتخاب‌شده",
  ["Select a snapshot first."]="ابتدا یک سابقه انتخاب کنید.",
  ["Account"]="حساب",
  ["AccountCode"]="کد حساب",
  ["AccountName"]="نام حساب",
  ["Direction"]="جهت",
  ["Number"]="شماره",
  ["Password"]="رمز عبور",
  ["Save"]="ذخیره",
  ["Cancel"]="انصراف",
  ["Close"]="بستن",
  ["Create"]="ایجاد",
  ["Delete"]="حذف",
  ["Search"]="جست‌وجو",
  ["Invoice"]="فاکتور",
  ["InvoiceId"]="شناسه فاکتور",
  ["CustomerId"]="شناسه مشتری",
  ["SupplierId"]="شناسه تأمین‌کننده",
  ["TotalAmount"]="مبلغ کل",
  ["NetAmount"]="مبلغ خالص",
  ["DiscountPercent"]="درصد تخفیف",
  ["TaxPercent"]="درصد مالیات",
  ["StockItem"]="کالا",
  ["SKU"]="کد کالا",
  ["UnitPrice"]="قیمت واحد",
  ["LineTotal"]="جمع ردیف",
  ["PaymentMethod"]="روش پرداخت",
  ["AmountDue"]="مبلغ قابل پرداخت",
  ["CreatedBy"]="ایجادکننده",
  ["Chart of accounts"]="کدینگ حساب‌ها",
  ["Search accounts by code, name or type"]="جست‌وجو بر اساس کد، نام یا نوع حساب",
  ["Add an account"]="افزودن حساب",
  ["+ Add account"]="افزودن حساب",
  ["Post a journal entry"]="ثبت سند روزنامه",
  ["Description"]="شرح",
  ["Debit account"]="حساب بدهکار",
  ["Credit account"]="حساب بستانکار",
  ["Amount"]="مبلغ",
  ["✓ Post balanced entry"]="ثبت سند تراز",
  ["Recent entries"]="اسناد اخیر",
  ["Customers & suppliers"]="مشتریان و تأمین‌کنندگان",
  ["Manage your business contacts in one place."]="مدیریت یکپارچه طرف حساب‌ها",
  ["Add contact"]="افزودن طرف حساب",
  ["Name"]="نام",
  ["Type"]="نوع",
  ["Phone"]="تلفن",
  ["Email"]="ایمیل",
  ["+ Save contact"]="ذخیره طرف حساب",
  ["Inventory catalog"]="فهرست کالاها",
  ["Add inventory item"]="افزودن کالا",
  ["Product name"]="نام کالا",
  ["Opening quantity"]="موجودی اولیه",
  ["Unit price"]="قیمت واحد",
  ["+ Save item"]="ذخیره کالا",
  ["Stock movements"]="گردش انبار",
  ["Trial balance"]="تراز آزمایشی",
  ["Profit and loss"]="سود و زیان",
  ["Account statement"]="گردش حساب",
  ["Search contacts"]="جست‌وجوی طرف حساب",
  ["Business name"]="نام کسب‌وکار",
  ["Change password"]="تغییر رمز عبور",
  ["Current password"]="رمز عبور فعلی",
  ["New password (10+ characters)"]="رمز جدید (حداقل ۱۰ نویسه)",
  ["Confirm new password"]="تکرار رمز جدید",
  ["Update password"]="ثبت رمز جدید",
  ["Password updated successfully."]="رمز عبور با موفقیت تغییر کرد.",
  ["Password update"]="تغییر رمز عبور",
  ["Finance Workspace"]="محیط مالی",
  ["Financial command center"]="مرکز فرماندهی مالی",
  ["Clarity for every transaction • Double-entry bookkeeping"]="شفافیت در هر تراکنش • حسابداری دوطرفه",
  ["● LOCAL DATABASE"]="● پایگاه داده محلی",
  ["Stage 18 • Bank reconciliation"]="نسخه ۲۲ • حسابداری صنعتی",
  ["Search by code, name or type"]="جست‌وجو با کد، نام یا نوع",
  ["Asset"]="دارایی",
  ["Liability"]="بدهی",
  ["Equity"]="حقوق مالکانه",
  ["Revenue"]="درآمد",
  ["Expense"]="هزینه",
  ["Customer"]="مشتری",
  ["Supplier"]="تأمین‌کننده",
  ["Receipt"]="دریافت",
  ["Payment"]="پرداخت",
  ["Cash"]="نقد",
  ["Bank"]="بانک",
  ["Received"]="دریافتی",
  ["Issued"]="پرداختی",
  ["Pending"]="در انتظار",
  ["Cleared"]="وصول‌شده",
  ["Returned"]="برگشتی",
  ["Cancelled"]="لغوشده",
  ["In"]="ورود",
  ["Out"]="خروج",
  ["Id"]="شناسه",
  ["Date"]="تاریخ",
  ["Code"]="کد",
  ["Balance"]="مانده",
  ["Debit"]="بدهکار",
  ["Credit"]="بستانکار",
  ["Reference"]="شماره پیگیری",
  ["Status"]="وضعیت",
  ["Quantity"]="تعداد",
  ["Price"]="قیمت",
  ["Kind"]="نوع",
  ["Channel"]="روش",
  ["OccurredAt"]="زمان ثبت",
  ["DueDate"]="سررسید",
  ["BankName"]="نام بانک",
  ["CheckNumber"]="شماره چک",
  ["Notes"]="یادداشت",
  ["Posted"]="ثبت نهایی",
  ["Partner"]="طرف حساب",
  ["JournalEntryId"]="شماره سند",
  ["StockItemId"]="شناسه کالا",
  ["UnitCost"]="بهای واحد",
  ["Total"]="جمع",
  ["Tax"]="مالیات",
  ["Discount"]="تخفیف",
  ["Username"]="نام کاربری",
  ["Role"]="نقش",
  ["CreatedAt"]="زمان ایجاد",
  ["AccountId"]="شناسه حساب",
  ["Source"]="منبع",
  ["Matched"]="تطبیق‌یافته",
  ["BankOnly"]="فقط بانک",
  ["LocalOnly"]="فقط سیستم"
 };
 public static string Translate(string value,bool persian)
 {
  if(!persian || string.IsNullOrWhiteSpace(value))return value;
  var trimmed=value.Trim();
  if(Fa.TryGetValue(trimmed,out var translated))return value.Replace(trimmed,translated);
  return value;
 }
 private sealed class OriginalText { public string? Text; public string? Tooltip; public string? Content; public string? AccessibleName; }
 private static readonly ConditionalWeakTable<DependencyObject,OriginalText> Originals=new();
 private static readonly ConditionalWeakTable<DataGridColumn,OriginalText> ColumnOriginals=new();
 public static string DisplayDate(DateTime date,bool persian)
 {
  if(!persian)return date.ToString("yyyy/MM/dd",CultureInfo.GetCultureInfo("en-US"));
  var calendar=new PersianCalendar();
  return $"{calendar.GetYear(date):0000}/{calendar.GetMonth(date):00}/{calendar.GetDayOfMonth(date):00}";
 }
 public static string DisplayAmount(decimal value,bool persian)
  => value.ToString("N2",CultureInfo.GetCultureInfo(persian?"fa-IR":"en-US"));
 public static void Apply(DependencyObject root,bool persian)
 {
  var original=Originals.GetValue(root,_=>new OriginalText());
  if(root is TextBlock label)
  {
   original.Text ??= label.Text;
   label.Text=Translate(original.Text,persian);
  }
  if(root is Button button && button.Tag is null && button.Content is string content)
  {
   original.Content ??= content;
   button.Content=Translate(original.Content,persian);
  }
  if(root is Control control && control.ToolTip is string tip)
  {
   original.Tooltip ??= tip;
   control.ToolTip=Translate(original.Tooltip,persian);
  }
  if(root is FrameworkElement accessible)
  {
   var name=System.Windows.Automation.AutomationProperties.GetName(accessible);
   if(!string.IsNullOrWhiteSpace(name))
   {
    original.AccessibleName ??= name;
    System.Windows.Automation.AutomationProperties.SetName(accessible,Translate(original.AccessibleName,persian));
   }
  }
  if(root is DataGrid grid)
  {
   grid.AutoGeneratingColumn-=GridAutoGeneratingColumn;
   grid.AutoGeneratingColumn+=GridAutoGeneratingColumn;
   grid.Tag=persian;
   foreach(var column in grid.Columns)
   {
    var originalHeader=ColumnOriginals.GetValue(column,_=>new OriginalText());
    originalHeader.Text ??= column.Header?.ToString();
    if(originalHeader.Text is not null)column.Header=Translate(originalHeader.Text,persian);
   }
  }
  if(root is FrameworkElement element)element.FlowDirection=persian?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
  for(int i=0;i<VisualTreeHelper.GetChildrenCount(root);i++)Apply(VisualTreeHelper.GetChild(root,i),persian);
 }
 private static void GridAutoGeneratingColumn(object? sender,DataGridAutoGeneratingColumnEventArgs e)
 {
  if(sender is not DataGrid grid)return;
  var fa=grid.Tag is true;
  e.Column.Header=Translate(e.PropertyName,fa);
  if(e.PropertyType==typeof(DateTime)||e.PropertyType==typeof(DateTime?))
  {
   if(e.Column is DataGridTextColumn dateColumn)
    dateColumn.Binding=new System.Windows.Data.Binding(e.PropertyName){Converter=new LocalizedDateConverter(fa)};
  }
 }
 private sealed class LocalizedDateConverter(bool fa):System.Windows.Data.IValueConverter
 {
  public object Convert(object value,Type targetType,object parameter,CultureInfo culture)
   => value is DateTime date?DisplayDate(date,fa):"";
  public object ConvertBack(object value,Type targetType,object parameter,CultureInfo culture)
   => System.Windows.Data.Binding.DoNothing;
 }
}
