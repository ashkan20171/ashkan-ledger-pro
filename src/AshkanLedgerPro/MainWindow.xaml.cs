using AshkanLedgerPro.Localization;
using AshkanLedgerPro.Security;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Media;
using Microsoft.Win32;
using AshkanLedgerPro.Services;
using AshkanLedgerPro.Models;
namespace AshkanLedgerPro;
public partial class MainWindow : Window {
 readonly AppUser currentUser;
 readonly AuthService auth=new();
 readonly LedgerService ledger=new();
 readonly BusinessService business=new();
 readonly InvoiceService invoices=new();
 readonly PurchaseService purchases=new();
 readonly TreasuryService treasury=new();
 readonly CheckService checks=new();
 readonly ReconciliationHistoryService reconciliationHistory=new();
 public MainWindow(AppUser user){currentUser=user;InitializeComponent();Title+=$" | {user.Username} ({user.Role})";try{ledger.Initialize();business.Initialize();invoices.Initialize();purchases.Initialize();treasury.Initialize();checks.Initialize();reconciliationHistory.Initialize();_ = new FiscalPeriodService().List();FiscalOperationalGuards.Install();ApplyPermissions();lightTheme=ThemePreferences.IsLight();ApplyTheme();ShowOverview();if(LanguagePreferences.IsPersian())Language_Click(this,new RoutedEventArgs());}catch(Exception ex){throw new InvalidOperationException("Unable to initialize accounting workspace.", ex);}}
 bool persian;
 Action? currentPage;
 void SetWorkspace(StackPanel panel){Workspace.Content=new ScrollViewer{Content=panel};UiTranslator.Apply(panel,persian);}

 readonly string[] navFa={"نمای کلی","کدینگ حساب‌ها","اسناد روزنامه","گردش حساب","تراز آزمایشی","سود و زیان","مشتریان و تأمین‌کنندگان","کالاها","گردش انبار","فاکتور فروش","فاکتور خرید","دریافت و پرداخت","مدیریت چک‌ها","مغایرت‌گیری بانکی","مدیریت کاربران","پشتیبان‌گیری","خروجی CSV"};
 readonly string[] navEn={"Overview","Chart of Accounts","Journal Entries","Account Statement","Trial Balance","Profit & Loss","Customers & Suppliers","Inventory Catalog","Stock Movements","Sales Invoices","Purchase Invoices","Receipts & Payments","Check Register","Bank Reconciliation","User Management","Backup Database","Export Journal CSV"};
 readonly string[] navKeys={"Overview","Accounts","Journal","Statement","Trial","Profit","Partners","Inventory","Movements","Invoices","Purchases","Treasury","Checks","Reconcile","Users","Backup","Export"};
 bool lightTheme;
 readonly Dictionary<string, string> darkPalette = new() { ["Bg"]="#091322", ["Panel"]="#15263B", ["Sidebar"]="#101F33", ["TextPrimary"]="#FFFFFF", ["TextMuted"]="#BED0E1", ["StatPanel"]="#1C3348", ["Accent"]="#26D2B0" };
 readonly Dictionary<string, string> lightPalette = new() { ["Bg"]="#EEF4FA", ["Panel"]="#FFFFFF", ["Sidebar"]="#E1EAF4", ["TextPrimary"]="#13263A", ["TextMuted"]="#465C71", ["StatPanel"]="#FFFFFF", ["Accent"]="#087E72" };
 void ApplyTheme(){
  var palette=lightTheme?lightPalette:darkPalette;
  // Resource brushes may be frozen by WPF; never mutate a shared brush in place.
  // Replacing the resource is safe for frozen brushes and updates DynamicResource bindings.
  foreach (var item in palette)
  {
   var color = (Color)ColorConverter.ConvertFromString(item.Value);
   Application.Current.Resources[item.Key] = new SolidColorBrush(color);
  }
  Background=(Brush)Application.Current.Resources["Bg"];
  ThemeButton.Content=T(lightTheme?"☾ Dark":"☀ Light",lightTheme?"☾ تیره":"☀ روشن");
 }
 string T(string en,string fa)=>persian?fa:en;
 void Language_Click(object sender,RoutedEventArgs e){
  persian=!persian;
  try { LanguagePreferences.Save(persian); } catch (IOException) { } catch (UnauthorizedAccessException) { }
  FlowDirection=persian?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
  System.Threading.Thread.CurrentThread.CurrentCulture=CultureInfo.GetCultureInfo(persian?"fa-IR":"en-US");
  System.Threading.Thread.CurrentThread.CurrentUICulture=CultureInfo.GetCultureInfo(persian?"fa-IR":"en-US");
  LanguageButton.Content=persian?"English":"فارسی";
  foreach(var button in FindButtons(this)){
   var tag=button.Tag?.ToString();
   int index=tag=="Admin"?14:Array.IndexOf(navKeys,tag?.StartsWith("Nav:")==true?tag.Substring(4):"");
   if(index>=0)button.Content=(persian?navFa:navEn)[index];
  }
  foreach(var block in FindTextBlocks(this))
  {
   if(block.Tag is string english && english.StartsWith("I18N:"))block.Text=UiTranslator.Translate(english.Substring(5),persian);
  }
  PasswordHeaderButton.Content=T("Change password","تغییر رمز عبور");
  ApplyTheme();
  Title=T("Ashkan Ledger Pro | Financial Command Center","اشکان لجر پرو | مرکز مدیریت مالی")+$" | {currentUser.Username}";
  if(currentPage!=null)currentPage();else ShowOverview();
 }
 static IEnumerable<TextBlock> FindTextBlocks(DependencyObject root){for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++){var child=System.Windows.Media.VisualTreeHelper.GetChild(root,i);if(child is TextBlock t)yield return t;foreach(var nested in FindTextBlocks(child))yield return nested;}}
 void Theme_Click(object sender,RoutedEventArgs e){
  lightTheme=!lightTheme;
  try { ThemePreferences.Save(lightTheme); } catch (IOException) { } catch (UnauthorizedAccessException) { }
  ApplyTheme();
  if(currentPage!=null)currentPage();else ShowOverview();
 }
 void Password_Click(object sender,RoutedEventArgs e){
  var dialog=new Window{Title=persian?"تغییر رمز عبور":"Change password",Owner=this,Width=430,Height=330,WindowStartupLocation=WindowStartupLocation.CenterOwner,Background=new SolidColorBrush(Color.FromRgb(16,31,51)),ResizeMode=ResizeMode.NoResize};
  var panel=new StackPanel{Margin=new Thickness(26)};
  var old=new PasswordBox{MinHeight=36,Margin=new Thickness(0,8,0,14)};
  var next=new PasswordBox{MinHeight=36,Margin=new Thickness(0,8,0,14)};
  var confirm=new PasswordBox{MinHeight=36,Margin=new Thickness(0,8,0,14)};
  panel.Children.Add(Info(T("Current password","رمز عبور فعلی")));panel.Children.Add(old);
  panel.Children.Add(Info(T("New password (10+ characters)","رمز جدید (حداقل ۱۰ نویسه)")));panel.Children.Add(next);
  panel.Children.Add(Info(T("Confirm new password","تکرار رمز جدید")));panel.Children.Add(confirm);
  var save=new Button{Content=T("Update password","ثبت رمز جدید"),MinHeight=38};
  save.Click+=(_,_)=>{try{if(next.Password!=confirm.Password)throw new ArgumentException("New passwords do not match.");if(!auth.ChangePassword(currentUser.Username,old.Password,next.Password))throw new ArgumentException("Current password is incorrect.");dialog.DialogResult=true;MessageBox.Show(T("Password updated successfully.","رمز عبور با موفقیت تغییر کرد."));}catch(Exception ex){ShowOperationError(ex, "Password update", "خطای عملیات");}};
  panel.Children.Add(save);dialog.Content=new ScrollViewer{Content=panel};dialog.ShowDialog();
 }
 void Reconcile_Click(object sender, RoutedEventArgs e)
 {currentPage=()=>Reconcile_Click(null!,new RoutedEventArgs());
  var p=Panel();
  p.Children.Add(Heading(T("Bank reconciliation preview","پیش‌نمایش مغایرت‌گیری بانکی")));
  p.Children.Add(Info(T("Import a bank CSV with headers Reference,Amount. Positive amounts are receipts; negative amounts are payments. This comparison does not modify accounting records.","فایل CSV بانک با ستون‌های Reference و Amount وارد کنید. مبلغ مثبت دریافت و مبلغ منفی پرداخت است. این بررسی هیچ تغییری در اسناد ایجاد نمی‌کند.")));
  var button=new Button{Content=T("Import bank CSV and compare","ورود CSV بانک و مقایسه"),Margin=new Thickness(0,14,0,14)};
  p.Children.Add(button);
  var result=new DataGrid{IsReadOnly=true,Height=320,AutoGenerateColumns=true};p.Children.Add(result);
  var save=new Button{Content=T("Save comparison snapshot","ذخیره نتیجه مغایرت‌گیری"),Margin=new Thickness(0,8,0,8),IsEnabled=false};p.Children.Add(save);
  var history=new DataGrid{IsReadOnly=true,Height=190,AutoGenerateColumns=true};p.Children.Add(Heading(T("Previous comparison snapshots","سوابق مغایرت‌گیری")));p.Children.Add(history);
  var viewSnapshot=new Button{Content=T("View selected snapshot details","نمایش جزئیات مغایرت‌گیری انتخاب‌شده"),Margin=new Thickness(0,8,0,8)};
  p.Children.Add(viewSnapshot);
  viewSnapshot.Click+=(_,_)=>{
   if(history.SelectedItem is not ReconciliationHistoryService.Snapshot selected){MessageBox.Show(T("Select a snapshot first.","ابتدا یک سابقه انتخاب کنید."));return;}
   try{
    var rows=reconciliationHistory.GetRows(selected.Id);
    var dialog=new Window{Title=T("Reconciliation snapshot details","جزئیات مغایرت‌گیری"),Owner=this,Width=800,Height=480,WindowStartupLocation=WindowStartupLocation.CenterOwner};
    dialog.Content=new DataGrid{ItemsSource=rows,IsReadOnly=true,AutoGenerateColumns=true};
    dialog.ShowDialog();
   }catch(Exception ex){ShowOperationError(ex, "Read error", "خطای خواندن");}
  };
  void RefreshHistory()=>history.ItemsSource=reconciliationHistory.Recent();
  RefreshHistory();
  List<BankReconciliation.Result>? currentRows=null;string? currentFile=null;int bankCount=0,ledgerCount=0;
  save.Click+=(_,_)=>{if(!CanEdit()||currentRows is null||currentFile is null)return;try{reconciliationHistory.Save(currentFile,currentUser.Username,bankCount,ledgerCount,currentRows);save.IsEnabled=false;RefreshHistory();MessageBox.Show(T("Snapshot saved. Accounting records were not changed.","نتیجه ذخیره شد. اسناد حسابداری تغییری نکردند."));}catch(Exception ex){ShowOperationError(ex, "Save error", "خطای ذخیره");}};
  button.Click+=(_,_)=>{
   try{
    var dialog=new OpenFileDialog{Filter="CSV files (*.csv)|*.csv"};
    if(dialog.ShowDialog()!=true)return;
    var bank=BankReconciliation.ReadBankCsv(dialog.FileName);
    var local=treasury.Recent().Where(x=>x.Channel=="Bank").Select(x=>new BankReconciliation.Entry(x.Reference,x.Kind=="Receipt"?x.Amount:-x.Amount)).ToList();
    var rows=BankReconciliation.Compare(bank,local);
    result.ItemsSource=rows;currentRows=rows;currentFile=dialog.FileName;bankCount=bank.Count;ledgerCount=local.Count;save.IsEnabled=CanEdit() && rows.Count>0;
    MessageBox.Show(T($"Compared {bank.Count} bank rows with {local.Count} local bank transactions. Unmatched: {rows.Count(x=>x.Status!="Matched")}",$"{bank.Count} ردیف بانکی و {local.Count} تراکنش داخلی بررسی شد. مغایرت‌ها: {rows.Count(x=>x.Status!="Matched")}"),T("Reconciliation preview","پیش‌نمایش مغایرت"));
   }catch(Exception ex){ShowOperationError(ex, "CSV import error", "خطای ورود CSV");}
  };
  SetWorkspace(p);
 }
 void Treasury_Click(object sender,RoutedEventArgs e)
 {currentPage=()=>Treasury_Click(null!,new RoutedEventArgs());
  var p=Panel();p.Children.Add(Heading(T("Receipts & payments","دریافت و پرداخت")));
  p.Children.Add(Info(T("Record cash or bank settlements and automatically post balanced journal entries.","ثبت دریافت و پرداخت نقدی یا بانکی با سند حسابداری خودکار")));
  // Read-only overview based on posted treasury transactions and pending checks.
  var summary=TreasuryInsights.Calculate(treasury.Recent(),checks.Recent(),DateTime.Today);
  var metrics=new WrapPanel{Margin=new Thickness(0,12,0,16)};
  void AddMetric(string english,string farsi,decimal value)
  {
   var card=new Border{Background=new SolidColorBrush(Color.FromRgb(25,49,72)),CornerRadius=new CornerRadius(12),Margin=new Thickness(0,0,12,12),Padding=new Thickness(16),MinWidth=175};
   var stack=new StackPanel();
   stack.Children.Add(Info(T(english,farsi)));
   stack.Children.Add(new TextBlock{Text=value.ToString("N2",CultureInfo.CurrentCulture),Foreground=Brushes.White,FontSize=23,FontWeight=FontWeights.SemiBold});
   card.Child=stack;metrics.Children.Add(card);
  }
  AddMetric("Cash receipts","دریافت نقدی",summary.CashReceipts);
  AddMetric("Cash payments","پرداخت نقدی",summary.CashPayments);
  AddMetric("Bank receipts","دریافت بانکی",summary.BankReceipts);
  AddMetric("Bank payments","پرداخت بانکی",summary.BankPayments);
  AddMetric("Net recorded cash flow","خالص جریان نقد ثبت‌شده",summary.NetCashFlow);
  AddMetric("Pending received checks","چک‌های دریافتی باز",summary.PendingReceivedChecks);
  AddMetric("Pending issued checks","چک‌های پرداختی باز",summary.PendingIssuedChecks);
  p.Children.Add(metrics);
  p.Children.Add(Info(T($"Overdue pending checks: {summary.OverdueChecks}",$"تعداد چک‌های سررسیدگذشته: {summary.OverdueChecks}")));
  p.Children.Add(Info(T("Indicators cover all recorded transactions, not actual bank balances. Check amounts are shown separately to avoid double counting.","شاخص‌ها بر اساس کل تراکنش‌های ثبت‌شده هستند، نه مانده واقعی بانک. مبالغ چک جدا نمایش داده می‌شوند تا دوباره‌شماری نشوند.")));

  var kind=new ComboBox{ItemsSource=new[]{"Receipt","Payment"},SelectedIndex=0};
  var channel=new ComboBox{ItemsSource=new[]{"Cash","Bank"},SelectedIndex=0};
  var partner=new ComboBox{DisplayMemberPath="Name"};
  void RefreshPartners(){var k=kind.SelectedItem?.ToString()=="Receipt"?"Customer":"Supplier";var partners=business.Partners().Where(x=>x.Kind==k).ToList();partner.ItemsSource=partners;partner.SelectedIndex=partners.Count>0?0:-1;}
  kind.SelectionChanged+=(_,_)=>RefreshPartners();RefreshPartners();
  var amount=new TextBox{Text="0.00"};var reference=new TextBox();
  p.Children.Add(Info(T("Transaction type","نوع تراکنش")));p.Children.Add(kind);
  p.Children.Add(Info(T("Cash or bank","صندوق یا بانک")));p.Children.Add(channel);
  p.Children.Add(Info(T("Customer / supplier","مشتری / تأمین‌کننده")));p.Children.Add(partner);
  p.Children.Add(Info(T("Amount","مبلغ")));p.Children.Add(amount);
  p.Children.Add(Info(T("Reference","شرح / شماره پیگیری")));p.Children.Add(reference);
  var save=new Button{Content=T("Post transaction","ثبت تراکنش"),Margin=new Thickness(0,16,0,20)};
  save.Click+=(_,_)=>{if(!CanEdit())return;try{
   if(partner.SelectedItem is not BusinessPartner bp)throw new ArgumentException("Select a partner.");
   if(!decimal.TryParse(amount.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var value) && !decimal.TryParse(amount.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out value))throw new ArgumentException("Invalid amount.");
   treasury.Record(bp.Id,kind.SelectedItem?.ToString()??"",channel.SelectedItem?.ToString()??"",value,reference.Text);
   Treasury_Click(sender,e);
  }catch(Exception ex){ShowOperationError(ex, "Treasury validation", "خطای عملیات");}};
  p.Children.Add(save);p.Children.Add(Heading(T("Recent settlements","آخرین تراکنش‌ها")));
  p.Children.Add(new DataGrid{ItemsSource=treasury.Recent().Select(x=>new{x.Id,x.OccurredAt,x.Kind,x.Channel,Partner=x.Partner?.Name,x.Amount,x.Reference,x.JournalEntryId}).ToList(),IsReadOnly=true,Height=280});
  SetWorkspace(p);
 }
 void Checks_Click(object sender,RoutedEventArgs e)
 {currentPage=()=>Checks_Click(null!,new RoutedEventArgs());
  var p=Panel();p.Children.Add(Heading(T("Check register & due dates","مدیریت چک‌ها و سررسیدها")));
  p.Children.Add(Info(T("Track checks and statuses. Status changes do NOT post bank or accounting entries.","ثبت چک و وضعیت آن؛ تغییر وضعیت به‌تنهایی سند بانکی یا حسابداری ایجاد نمی‌کند.")));
  var direction=new ComboBox{ItemsSource=new[]{"Received","Issued"},SelectedIndex=0};
  var partner=new ComboBox{DisplayMemberPath="Name"};
  void ReloadPartners(){var kind=direction.SelectedItem?.ToString()=="Received"?"Customer":"Supplier";var items=business.Partners().Where(x=>x.Kind==kind).ToList();partner.ItemsSource=items;partner.SelectedIndex=items.Count>0?0:-1;}
  direction.SelectionChanged+=(_,_)=>ReloadPartners();ReloadPartners();
  var number=new TextBox();var bank=new TextBox();var amount=new TextBox();var due=new TextBox{Text=LocalizedDateInput.Format(DateTime.Today.AddDays(30),persian)};var notes=new TextBox();
  p.Children.Add(Info(T("Direction","نوع چک")));p.Children.Add(direction);
  p.Children.Add(Info(T("Customer / supplier","طرف حساب")));p.Children.Add(partner);
  p.Children.Add(Info(T("Check number","شماره چک")));p.Children.Add(number);
  p.Children.Add(Info(T("Bank name","نام بانک")));p.Children.Add(bank);
  p.Children.Add(Info(T("Amount","مبلغ")));p.Children.Add(amount);
  p.Children.Add(Info(T("Due date (YYYY/MM/DD)","تاریخ سررسید شمسی (سال/ماه/روز)")));
  var dateRow = new DockPanel { LastChildFill = true };
  var pickDate = new Button { Content = T("Choose date", "انتخاب از تقویم"), Margin = new Thickness(8, 0, 0, 0), MinWidth = 135 };
  DockPanel.SetDock(pickDate, Dock.Right);
  dateRow.Children.Add(pickDate);
  dateRow.Children.Add(due);
  pickDate.Click += (_, _) => {
   var initial = LocalizedDateInput.TryParse(due.Text, persian, out var parsed) ? parsed : DateTime.Today;
   var picker = new LocalizedCalendarDialog(initial, persian) { Owner = this };
   if (picker.ShowDialog() == true) due.Text = LocalizedDateInput.Format(picker.SelectedDate, persian);
  };
  p.Children.Add(dateRow);
  p.Children.Add(Info(T("Notes","توضیحات")));p.Children.Add(notes);
  var add=new Button{Content=T("Register check","ثبت چک"),Margin=new Thickness(0,12,0,12)};
  add.IsEnabled=currentUser.Role!="Viewer";
  add.Click+=(_,_)=>{if(!CanEdit())return;try{
   if(partner.SelectedItem is not BusinessPartner bp)throw new ArgumentException("Select a partner.");
   if(!decimal.TryParse(amount.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var value) && !decimal.TryParse(amount.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out value))throw new ArgumentException("Invalid amount.");
   if(!LocalizedDateInput.TryParse(due.Text,persian,out DateTime date))throw new ArgumentException(T("Invalid Gregorian date. Use YYYY/MM/DD.","تاریخ شمسی نامعتبر است. قالب سال/ماه/روز را وارد کنید."));
   checks.Add(bp.Id,direction.SelectedItem?.ToString()??"",number.Text,bank.Text,value,date,notes.Text);
   Checks_Click(sender,e);
  }catch(Exception ex){ShowOperationError(ex, "Check validation", "اعتبارسنجی چک");}};p.Children.Add(add);
  var all=checks.Recent();var pending=all.Where(x=>x.Status=="Pending").ToList();
  var stats=new WrapPanel();stats.Children.Add(Stat(T("PENDING CHECKS","چک‌های باز"),pending.Count.ToString(),lightTheme?"#135C93":"#A8D8FF"));
  stats.Children.Add(Stat(T("OVERDUE","سررسید گذشته"),pending.Count(x=>x.DueDate.Date<DateTime.Today).ToString(),lightTheme?"#925300":"#FFC178"));
  stats.Children.Add(Stat(T("RECEIVABLE","دریافتی باز"),pending.Where(x=>x.Direction=="Received").Sum(x=>x.Amount).ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
  stats.Children.Add(Stat(T("PAYABLE","پرداختی باز"),pending.Where(x=>x.Direction=="Issued").Sum(x=>x.Amount).ToString("N2"),lightTheme?"#925300":"#FFC178"));p.Children.Add(stats);
  p.Children.Add(Heading(T("Check history","سوابق چک‌ها")));
  var grid=new DataGrid{ItemsSource=all.Select(x=>new CheckRow(x.Id,x.Direction,x.Number,x.Bank,x.Partner?.Name??"",x.Amount,x.DueDate,x.Status)).ToList(),IsReadOnly=true,Height=300,SelectionMode=DataGridSelectionMode.Single};p.Children.Add(grid);
  var status=new ComboBox{ItemsSource=new[]{"Cleared","Bounced","Cancelled"},SelectedIndex=0};p.Children.Add(Info(T("Set selected pending check status","تغییر وضعیت چک انتخاب‌شده")));p.Children.Add(status);
  var update=new Button{Content=T("Update status","ثبت وضعیت"),IsEnabled=currentUser.Role!="Viewer",Margin=new Thickness(0,10,0,10)};
  update.Click+=(_,_)=>{if(!CanEdit())return;try{if(grid.SelectedItem is not CheckRow row)throw new ArgumentException("Select a check.");
   if(MessageBox.Show(T("Update check status? This does not create a journal entry.","وضعیت چک تغییر کند؟ این عملیات سند حسابداری ایجاد نمی‌کند."),T("Confirm","تأیید"),MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;
   checks.SetStatus(row.Id,status.SelectedItem?.ToString()??"",currentUser.Username);Checks_Click(sender,e);
  }catch(Exception ex){ShowOperationError(ex, "Check status", "خطای عملیات");}};p.Children.Add(update);
  var historyButton=new Button{Content=T("View selected check history","تاریخچه وضعیت چک انتخاب‌شده"),Margin=new Thickness(0,8,0,8)};
  historyButton.Click+=(_,_)=>{if(grid.SelectedItem is not CheckRow selected){MessageBox.Show(T("Select a check first.","ابتدا یک چک انتخاب کنید."));return;}
   var records=checks.History(selected.Id);var win=new Window{Title=T("Check status audit history","تاریخچه تغییر وضعیت چک"),Owner=this,Width=720,Height=390,WindowStartupLocation=WindowStartupLocation.CenterOwner};
   win.Content=new DataGrid{ItemsSource=records,IsReadOnly=true,AutoGenerateColumns=true};win.ShowDialog();};p.Children.Add(historyButton);
  var export=new Button{Content=T("Export checks to CSV","خروجی CSV چک‌ها"),Margin=new Thickness(0,8,0,8)};
  export.Click+=(_,_)=>{try{var dlg=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName="checks.csv"};if(dlg.ShowDialog()!=true)return;
   static string EscapeCsv(string? value){var safe=value??"";if(safe.Length>0 && "=+-@".Contains(safe[0]))safe="'"+safe;return "\""+safe.Replace("\"","\"\"")+"\"";}
   var lines=new List<string>{"Id,Direction,Number,Bank,Partner,Amount,DueDate,Status"};
   foreach(var x in all)lines.Add(string.Join(",",new[]{x.Id.ToString(CultureInfo.InvariantCulture),EscapeCsv(x.Direction),EscapeCsv(x.Number),EscapeCsv(x.Bank),EscapeCsv(x.Partner?.Name),x.Amount.ToString("0.00",CultureInfo.InvariantCulture),x.DueDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),EscapeCsv(x.Status)}));
   File.WriteAllLines(dlg.FileName,lines,new UTF8Encoding(true));MessageBox.Show(T("CSV exported successfully.","فایل CSV ذخیره شد."));
  }catch(Exception ex){ShowOperationError(ex, "CSV export", "خطای عملیات");}};p.Children.Add(export);
  SetWorkspace(p);
 }
 private sealed record CheckRow(int Id,string Direction,string Number,string Bank,string Partner,decimal Amount,DateTime DueDate,string Status);
 void ApplyPermissions(){bool admin=currentUser.Role=="Admin";bool edit=admin||currentUser.Role=="Accountant";
 foreach(var button in FindButtons(this)){if(button.Tag?.ToString()=="Admin")button.Visibility=admin?Visibility.Visible:Visibility.Collapsed;else if(button.Tag?.ToString()=="Edit")button.Visibility=edit?Visibility.Visible:Visibility.Collapsed;}}
 static IEnumerable<Button> FindButtons(DependencyObject root){for(int i=0;i<System.Windows.Media.VisualTreeHelper.GetChildrenCount(root);i++){var child=System.Windows.Media.VisualTreeHelper.GetChild(root,i);if(child is Button b)yield return b;foreach(var nested in FindButtons(child))yield return nested;}}
 void ShowOperationError(Exception ex, string englishTitle, string persianTitle)
 {
  try
  {
   var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "AshkanLedgerPro", "Logs");
   Directory.CreateDirectory(directory);
   File.AppendAllText(Path.Combine(directory, "operations.log"),
    $"[{DateTime.UtcNow:O}] {englishTitle}: {ex}\n", Encoding.UTF8);
  }
  catch (IOException) { }
  catch (UnauthorizedAccessException) { }
  if (ex.ToString().Contains("Fiscal period locked", StringComparison.OrdinalIgnoreCase))
  {
   try { new SecurityEventService().Record(currentUser.Username, "FiscalWriteDenied", englishTitle, ex.Message); }
   catch (Exception logError) { System.Diagnostics.Debug.WriteLine($"Security event logging failed: {logError.Message}"); }
  }
  var message = persian
   ? "عملیات انجام نشد. اطلاعات واردشده را بررسی کنید. در صورت تکرار خطا، گزارش برنامه را بررسی کنید."
   : ex.Message;
  MessageBox.Show(message, T(englishTitle, persianTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
 }
 bool CanEdit(){if(currentUser.Role!="Viewer")return true;MessageBox.Show(T("Viewer role is read-only.","نقش مشاهده‌گر فقط اجازه مشاهده اطلاعات را دارد."));return false;}

 TextBlock Heading(string text)=>new(){Text=text,FontSize=25,FontWeight=FontWeights.Bold,Foreground=(Brush)Application.Current.Resources["TextPrimary"],Margin=new Thickness(0,0,0,18)};
 TextBlock Info(string text)=>new(){Text=text,Foreground=(Brush)Application.Current.Resources["TextMuted"],FontSize=14,Margin=new Thickness(0,6,0,6),TextWrapping=TextWrapping.Wrap};
 StackPanel Panel()=>new(){Margin=new Thickness(0,0,8,0)};
 Border Stat(string label,string value,string color){var s=new StackPanel();s.Children.Add(new TextBlock{Text=label,Foreground=(Brush)Application.Current.Resources["TextMuted"],FontSize=13});s.Children.Add(new TextBlock{Text=value,Foreground=(Brush)new BrushConverter().ConvertFromString(color)!,FontSize=25,FontWeight=FontWeights.Bold,Margin=new Thickness(0,9,0,0)});return new Border{Background=(Brush)Application.Current.Resources["StatPanel"],CornerRadius=new CornerRadius(14),Padding=new Thickness(19),Margin=new Thickness(0,0,12,12),Child=s,MinWidth=190};}
 void ShowOverview(){
  currentPage=ShowOverview;
  var p=Panel();
  p.Children.Add(Heading(T("Financial dashboard","داشبورد مالی")));
  p.Children.Add(Info(T("Live figures calculated from posted journal entries. All values use the ledger's original currency; no conversion is applied.","آمار بر اساس اسناد ثبت‌شده محاسبه می‌شود. تمام مبالغ به واحد اصلی دفتر هستند و تبدیل ارز انجام نمی‌شود.")));
  var accounts=ledger.Accounts();var entries=ledger.Entries();var balances=ledger.Balances();
  decimal Total(string type)=>accounts.Where(x=>x.Type==type).Sum(x=>balances.GetValueOrDefault(x.Id));
  var revenue=-Total("Revenue");var expense=Total("Expense");
  var refreshDashboard=new Button{Content=T("↻ Refresh dashboard","↻ به‌روزرسانی داشبورد"),MinWidth=170,HorizontalAlignment=HorizontalAlignment.Left,Margin=new Thickness(0,8,0,8)};
  refreshDashboard.Click+=(_,_)=>ShowOverview();
  p.Children.Add(refreshDashboard);
  var cards=new WrapPanel{Margin=new Thickness(0,18,0,18)};
  cards.Children.Add(Stat(T("ACCOUNTS","حساب‌ها"),accounts.Count.ToString("N0"),lightTheme?"#087E72":"#EAF5FF"));
  cards.Children.Add(Stat(T("JOURNAL ENTRIES","اسناد روزنامه"),entries.Count.ToString("N0"),lightTheme?"#087E72":"#EAF5FF"));
  cards.Children.Add(Stat(T("REVENUE","درآمد"),revenue.ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
  cards.Children.Add(Stat(T("EXPENSES","هزینه‌ها"),expense.ToString("N2"),lightTheme?"#925300":"#FFC178"));
  cards.Children.Add(Stat(T("NET PROFIT","سود خالص"),(revenue-expense).ToString("N2"),lightTheme?"#135C93":"#A8D8FF"));
  var profitMargin=revenue==0m?(decimal?)null:Math.Round((revenue-expense)/revenue*100m,2);
  cards.Children.Add(Stat(T("PROFIT MARGIN","حاشیه سود"),profitMargin.HasValue?profitMargin.Value.ToString("N2")+"%":T("N/A","نامشخص"),lightTheme?"#135C93":"#A8D8FF"));
  p.Children.Add(cards);
  // Read-only accounting quality indicators based on the current ledger snapshot.
  var unbalanced=entries.Count(entry=>entry.Lines.Sum(line=>line.Debit)!=entry.Lines.Sum(line=>line.Credit));
  var missingDescriptions=entries.Count(entry=>string.IsNullOrWhiteSpace(entry.Description));
  var debitTotal=entries.Sum(entry=>entry.Lines.Sum(line=>line.Debit));
  var creditTotal=entries.Sum(entry=>entry.Lines.Sum(line=>line.Credit));
  p.Children.Add(Heading(T("Ledger health checks","کنترل سلامت دفتر حسابداری")));
  var health=new WrapPanel{Margin=new Thickness(0,0,0,14)};
  health.Children.Add(Stat(T("UNBALANCED ENTRIES","اسناد نامتوازن"),unbalanced.ToString("N0"),unbalanced==0?lightTheme?"#087E72":"#26D2B0":lightTheme?"#925300":"#FFC178"));
  health.Children.Add(Stat(T("MISSING DESCRIPTIONS","اسناد بدون شرح"),missingDescriptions.ToString("N0"),missingDescriptions==0?lightTheme?"#087E72":"#26D2B0":lightTheme?"#925300":"#FFC178"));
  health.Children.Add(Stat(T("DEBIT / CREDIT DIFFERENCE","اختلاف بدهکار و بستانکار"),Math.Abs(debitTotal-creditTotal).ToString("N2"),debitTotal==creditTotal?lightTheme?"#087E72":"#26D2B0":lightTheme?"#925300":"#FFC178"));
  p.Children.Add(health);
  p.Children.Add(Info(T("Checks are informational and do not modify posted entries.","این کنترل‌ها فقط اطلاع‌رسانی می‌کنند و اسناد ثبت‌شده را تغییر نمی‌دهند.")));
  // Read-only reminders for pending checks; refreshed whenever the dashboard is opened.
  var dueAlerts=FinancialAlertService.CheckAlerts(checks.Recent(),DateTime.Today);
  p.Children.Add(Heading(T("Check reminders — overdue and next 14 days","یادآوری چک‌ها — معوق و ۱۴ روز آینده")));
  var alertCards=new WrapPanel{Margin=new Thickness(0,0,0,10)};
  var overdueCount=dueAlerts.Count(a=>a.Category=="Overdue");
  alertCards.Children.Add(Stat(T("OVERDUE CHECKS","چک‌های معوق"),overdueCount.ToString("N0"),overdueCount>0?(lightTheme?"#925300":"#FFC178"):(lightTheme?"#087E72":"#26D2B0")));
  alertCards.Children.Add(Stat(T("DUE TODAY","سررسید امروز"),dueAlerts.Count(a=>a.Category=="Today").ToString("N0"),lightTheme?"#135C93":"#A8D8FF"));
  alertCards.Children.Add(Stat(T("UPCOMING CHECKS","چک‌های پیش رو"),dueAlerts.Count(a=>a.Category=="Upcoming").ToString("N0"),lightTheme?"#087E72":"#26D2B0"));
  p.Children.Add(alertCards);
  if(dueAlerts.Count==0)p.Children.Add(Info(T("No pending checks due within the next 14 days or overdue.","چک معوق یا سررسیدشونده در ۱۴ روز آینده وجود ندارد.")));
  else
  {
   var alertControls=new WrapPanel{Margin=new Thickness(0,0,0,8)};
   var search=new TextBox{MinWidth=220,MinHeight=32,Margin=new Thickness(0,0,10,6),ToolTip=T("Search check number","جست‌وجوی شماره چک")};
   var filter=new ComboBox{MinWidth=160,MinHeight=32,Margin=new Thickness(0,0,10,6)};
   filter.Items.Add(T("All reminders","همه یادآوری‌ها"));
   filter.Items.Add(T("Overdue","معوق"));
   filter.Items.Add(T("Due today","سررسید امروز"));
   filter.Items.Add(T("Upcoming","پیش رو"));
   filter.SelectedIndex=0;
   var exportAlerts=new Button{Content=T("Export reminders CSV","خروجی CSV یادآوری‌ها"),Margin=new Thickness(0,0,10,6)};
   alertControls.Children.Add(search);alertControls.Children.Add(filter);alertControls.Children.Add(exportAlerts);
   p.Children.Add(alertControls);
   var alertGrid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=false,Height=220,Margin=new Thickness(0,0,0,12)};
   alertGrid.Columns.Add(new DataGridTextColumn{Header=T("Status","وضعیت"),Binding=new System.Windows.Data.Binding("Status")});
   alertGrid.Columns.Add(new DataGridTextColumn{Header=T("Check number","شماره چک"),Binding=new System.Windows.Data.Binding("Reference")});
   alertGrid.Columns.Add(new DataGridTextColumn{Header=T("Due date","سررسید"),Binding=new System.Windows.Data.Binding("DueDate")});
   alertGrid.Columns.Add(new DataGridTextColumn{Header=T("Amount","مبلغ"),Binding=new System.Windows.Data.Binding("Amount")});
   alertGrid.Columns.Add(new DataGridTextColumn{Header=T("Days remaining","روز تا سررسید"),Binding=new System.Windows.Data.Binding("Days")});
   List<FinancialAlertService.Alert> FilteredAlerts()=>dueAlerts.Where(a=>
     (filter.SelectedIndex==0 || a.Category==new[]{"", "Overdue", "Today", "Upcoming"}[filter.SelectedIndex]) &&
     a.Reference.Contains(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)).ToList();
   void RefreshAlerts()=>alertGrid.ItemsSource=FilteredAlerts().Select(a=>new {
     Status=a.Category=="Overdue"?T("Overdue","معوق"):a.Category=="Today"?T("Due today","سررسید امروز"):T("Upcoming","پیش رو"),
     a.Reference,DueDate=LocalizedDateInput.Format(a.DueDate,persian),Amount=a.Amount.ToString("N2"),Days=a.DaysFromToday
   }).ToList();
   search.TextChanged+=(_,_)=>RefreshAlerts();
   filter.SelectionChanged+=(_,_)=>RefreshAlerts();
   RefreshAlerts();
   exportAlerts.Click+=(_,_)=>{
    try{
     var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName="check-reminders.csv"};
     if(dialog.ShowDialog()!=true)return;
     static string Csv(string value)=>"\""+value.Replace("\"","\"\"")+"\"";
     var rows=new List<string>{"Category,Reference,DueDate,Amount,DaysFromToday"};
     rows.AddRange(FilteredAlerts().Select(a=>string.Join(",",new[]{Csv(a.Category),Csv(a.Reference),Csv(a.DueDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)),a.Amount.ToString(CultureInfo.InvariantCulture),a.DaysFromToday.ToString(CultureInfo.InvariantCulture)})));
     File.WriteAllLines(dialog.FileName,rows,new UTF8Encoding(true));
     MessageBox.Show(T("Reminders exported successfully.","یادآوری‌ها با موفقیت صادر شدند."));
    }catch(Exception ex){ShowOperationError(ex,"Export reminders","خطای خروجی یادآوری‌ها");}
   };
   p.Children.Add(alertGrid);
  }
  p.Children.Add(Info(T("Reminders are informational and do not settle or modify checks.","یادآوری‌ها فقط اطلاع‌رسانی هستند و وضعیت چک‌ها را تغییر نمی‌دهند.")));
  var actions=new WrapPanel{Margin=new Thickness(0,0,0,18)};
  void QuickAction(string en,string fa,RoutedEventHandler action){var b=new Button{Content=T(en,fa),MinWidth=150,Margin=new Thickness(0,0,10,8)};b.Click+=action;actions.Children.Add(b);}
  QuickAction("+ Journal entry","+ سند جدید",Journal_Click);
  QuickAction("Sales invoices","فاکتورهای فروش",Invoices_Click);
  QuickAction("Treasury","خزانه‌داری",Treasury_Click);
  QuickAction("Trial balance","تراز آزمایشی",Trial_Click);
  QuickAction("Global search","جست‌وجوی سراسری",GlobalSearch_Click);
  p.Children.Add(actions);
  p.Children.Add(Heading(T("Last six months — revenue and expenses","شش ماه اخیر — درآمد و هزینه")));
  p.Children.Add(Info(T("Based on journal lines posted to revenue and expense accounts. Monthly totals can be negative after reversals.","بر اساس ردیف‌های اسناد حساب‌های درآمد و هزینه؛ در صورت ثبت سند برگشتی، مبلغ ماهانه ممکن است منفی باشد.")));
  var monthStart=new DateTime(DateTime.Today.Year,DateTime.Today.Month,1);
  var revenueIds=accounts.Where(a=>a.Type=="Revenue").Select(a=>a.Id).ToHashSet();
  var expenseIds=accounts.Where(a=>a.Type=="Expense").Select(a=>a.Id).ToHashSet();
  var monthly=Enumerable.Range(0,6).Select(i=>{
    var start=monthStart.AddMonths(i-5);var finish=start.AddMonths(1);
    var lines=entries.Where(e=>e.Date>=start && e.Date<finish).SelectMany(e=>e.Lines).ToList();
    return (Month:start,Revenue:lines.Where(l=>revenueIds.Contains(l.AccountId)).Sum(l=>l.Credit-l.Debit),Expense:lines.Where(l=>expenseIds.Contains(l.AccountId)).Sum(l=>l.Debit-l.Credit));
  }).ToList();
  decimal scale=Math.Max(1m,monthly.SelectMany(m=>new[]{Math.Abs(m.Revenue),Math.Abs(m.Expense)}).Max());
  var chart=new UniformGrid{Columns=6,Margin=new Thickness(0,10,0,22)};
  foreach(var month in monthly){
    var group=new StackPanel{Margin=new Thickness(5,0,5,0)};
    var plot=new Grid{Height=150};
    plot.ColumnDefinitions.Add(new ColumnDefinition());plot.ColumnDefinitions.Add(new ColumnDefinition());
    Border Bar(decimal amount,string color)=>new(){Height=Math.Max(3,140*(double)(Math.Abs(amount)/scale)),VerticalAlignment=VerticalAlignment.Bottom,Background=(Brush)new BrushConverter().ConvertFromString(color)!,CornerRadius=new CornerRadius(6,6,0,0),Margin=new Thickness(3,0,3,0),ToolTip=amount.ToString("N2")};
    var r=Bar(month.Revenue,lightTheme?"#087E72":"#26D2B0");var e=Bar(month.Expense,lightTheme?"#925300":"#FFC178");Grid.SetColumn(e,1);plot.Children.Add(r);plot.Children.Add(e);group.Children.Add(plot);
    group.Children.Add(new TextBlock{Text=persian?new System.Globalization.PersianCalendar().GetYear(month.Month).ToString()+"/"+new System.Globalization.PersianCalendar().GetMonth(month.Month).ToString("00"):month.Month.ToString("MMM yy",CultureInfo.GetCultureInfo("en-US")),Foreground=(Brush)Application.Current.Resources["TextPrimary"],TextAlignment=TextAlignment.Center,Margin=new Thickness(0,7,0,0),FontSize=11});
    chart.Children.Add(group);
  }
  p.Children.Add(chart);
  // Six-month management snapshot; the selected range is the same as the chart above.
  var sixMonthRevenue=monthly.Sum(m=>m.Revenue);
  var sixMonthExpense=monthly.Sum(m=>m.Expense);
  var sixMonthNet=sixMonthRevenue-sixMonthExpense;
  var sixMonthMargin=sixMonthRevenue==0m?(decimal?)null:Math.Round(sixMonthNet/sixMonthRevenue*100m,2);
  var trendCards=new WrapPanel{Margin=new Thickness(0,0,0,12)};
  trendCards.Children.Add(Stat(T("6-MONTH REVENUE","درآمد شش‌ماهه"),sixMonthRevenue.ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
  trendCards.Children.Add(Stat(T("6-MONTH EXPENSES","هزینه شش‌ماهه"),sixMonthExpense.ToString("N2"),lightTheme?"#925300":"#FFC178"));
  trendCards.Children.Add(Stat(T("6-MONTH NET PROFIT","سود خالص شش‌ماهه"),sixMonthNet.ToString("N2"),lightTheme?"#135C93":"#A8D8FF"));
  trendCards.Children.Add(Stat(T("6-MONTH MARGIN","حاشیه سود شش‌ماهه"),sixMonthMargin.HasValue?sixMonthMargin.Value.ToString("N2")+"%":T("N/A","نامشخص"),lightTheme?"#135C93":"#A8D8FF"));
  p.Children.Add(trendCards);
  p.Children.Add(Info(T("Profit margin = net profit / revenue. N/A when revenue is zero. Negative reversals remain visible in the totals.","حاشیه سود = سود خالص تقسیم بر درآمد؛ در صورت صفر بودن درآمد، نامشخص نمایش داده می‌شود. آثار اسناد برگشتی در جمع‌ها حفظ می‌شود.")));
  var legend=new WrapPanel{Margin=new Thickness(0,0,0,18)};
  legend.Children.Add(Info(T("● Revenue (green)","● درآمد (سبز)")));
  legend.Children.Add(Info(T("   ● Expenses (orange)","   ● هزینه (نارنجی)")));
  p.Children.Add(legend);
  // Stage 85: custom-period dashboard comparison. Inclusive Gregorian dates, read-only.
  p.Children.Add(Heading(T("Compare custom periods","مقایسه بازه‌های دلخواه")));
  p.Children.Add(Info(T("Enter dates in your selected calendar (yyyy/MM/dd). The previous period has the same number of days immediately before the selected period.",
   "تاریخ‌ها را به شمسی (yyyy/MM/dd) وارد کنید. دوره قبل، بازه‌ای هم‌اندازه و بلافاصله پیش از دوره انتخابی است.")));
  var rangeControls=new WrapPanel{Margin=new Thickness(0,8,0,10)};
  var rangeFrom=new TextBox{Width=145,Margin=new Thickness(0,0,10,8),Text=LocalizedDateInput.Format(DateTime.Today.AddDays(-29),persian)};
  var rangeTo=new TextBox{Width=145,Margin=new Thickness(0,0,10,8),Text=LocalizedDateInput.Format(DateTime.Today,persian)};
  var rangeRefresh=new Button{Content=T("Compare periods","مقایسه دوره‌ها"),Margin=new Thickness(0,0,10,8)};
  var rangeExport=new Button{Content=T("Export comparison CSV","خروجی CSV مقایسه"),Margin=new Thickness(0,0,10,8)};
  rangeControls.Children.Add(rangeFrom);rangeControls.Children.Add(rangeTo);
  rangeControls.Children.Add(rangeRefresh);rangeControls.Children.Add(rangeExport);p.Children.Add(rangeControls);
  var rangeTotals=new WrapPanel{Margin=new Thickness(0,0,0,10)};p.Children.Add(rangeTotals);
  var comparisonChart=new WrapPanel{Margin=new Thickness(0,4,0,16)};p.Children.Add(comparisonChart);
  var rangeGrid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=true,Height=130,Margin=new Thickness(0,0,0,20)};
  p.Children.Add(rangeGrid);
  ManagementAnalyticsService.PeriodTotals? selectedPeriod=null,previousPeriod=null;
  void RefreshRange(){
   if(!LocalizedDateInput.TryParse(rangeFrom.Text,persian,out var from) ||
      !LocalizedDateInput.TryParse(rangeTo.Text,persian,out var to) || from>to)
    throw new ArgumentException(T("Enter a valid date range in the selected calendar.","بازه تاریخ شمسی معتبر وارد کنید."));
   var days=(to-from).Days+1;
   if(days>3660 || from<=DateTime.MinValue.AddDays(days))
    throw new ArgumentException(T("Select a range of at most ten years with a valid previous period.","بازه حداکثر ده‌ساله با دوره قبل معتبر انتخاب کنید."));
   selectedPeriod=ManagementAnalyticsService.ForPeriod(accounts,entries,from,to);
   previousPeriod=ManagementAnalyticsService.ForPeriod(accounts,entries,from.AddDays(-days),from.AddDays(-1));
   rangeTotals.Children.Clear();
   rangeTotals.Children.Add(Stat(T("REVENUE CHANGE","تغییر درآمد"),(selectedPeriod.Revenue-previousPeriod.Revenue).ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
   rangeTotals.Children.Add(Stat(T("EXPENSE CHANGE","تغییر هزینه"),(selectedPeriod.Expenses-previousPeriod.Expenses).ToString("N2"),lightTheme?"#925300":"#FFC178"));
   rangeTotals.Children.Add(Stat(T("PROFIT CHANGE","تغییر سود"),(selectedPeriod.NetProfit-previousPeriod.NetProfit).ToString("N2"),lightTheme?"#135C93":"#A8D8FF"));
   rangeTotals.Children.Add(Stat(T("REVENUE GROWTH","رشد درآمد"),ManagementAnalyticsService.GrowthLabel(selectedPeriod.Revenue,previousPeriod.Revenue),lightTheme?"#087E72":"#26D2B0"));
   rangeTotals.Children.Add(Stat(T("PROFIT GROWTH","رشد سود"),ManagementAnalyticsService.GrowthLabel(selectedPeriod.NetProfit,previousPeriod.NetProfit),lightTheme?"#135C93":"#A8D8FF"));
   comparisonChart.Children.Clear();
   decimal maxMagnitude=new[]{Math.Abs(selectedPeriod.Revenue),Math.Abs(previousPeriod.Revenue),Math.Abs(selectedPeriod.Expenses),Math.Abs(previousPeriod.Expenses),Math.Abs(selectedPeriod.NetProfit),Math.Abs(previousPeriod.NetProfit),1m}.Max();
   void CompareBar(string title,decimal previous,decimal selected,string color){
    var block=new StackPanel{Width=230,Margin=new Thickness(0,0,16,12)};
    block.Children.Add(new TextBlock{Text=title,FontWeight=FontWeights.SemiBold,Margin=new Thickness(0,0,0,6)});
    foreach(var (label,value) in new[]{(T("Previous","قبلی"),previous),(T("Selected","انتخابی"),selected)}){
     var barRow=new StackPanel{Margin=new Thickness(0,0,0,5)};
     barRow.Children.Add(new TextBlock{Text=$"{label}: {value:N2}",FontSize=11});
     barRow.Children.Add(new Border{Height=11,Width=Math.Max(3,200*(double)(Math.Abs(value)/maxMagnitude)),HorizontalAlignment=HorizontalAlignment.Left,Background=(Brush)new BrushConverter().ConvertFromString(color)!,CornerRadius=new CornerRadius(4),ToolTip=value.ToString("N2")});
     block.Children.Add(barRow);
    }
    comparisonChart.Children.Add(block);
   }
   CompareBar(T("Revenue","درآمد"),previousPeriod.Revenue,selectedPeriod.Revenue,lightTheme?"#087E72":"#26D2B0");
   CompareBar(T("Expenses","هزینه"),previousPeriod.Expenses,selectedPeriod.Expenses,lightTheme?"#925300":"#FFC178");
   CompareBar(T("Net profit","سود خالص"),previousPeriod.NetProfit,selectedPeriod.NetProfit,lightTheme?"#135C93":"#A8D8FF");
   rangeGrid.ItemsSource=new[]{
    new {Period=T("Selected","انتخابی"),Revenue=selectedPeriod.Revenue,Expenses=selectedPeriod.Expenses,NetProfit=selectedPeriod.NetProfit,Margin=selectedPeriod.MarginPercent.HasValue?selectedPeriod.MarginPercent.Value.ToString("N2")+"%":T("N/A","نامشخص")},
    new {Period=T("Previous","قبلی"),Revenue=previousPeriod.Revenue,Expenses=previousPeriod.Expenses,NetProfit=previousPeriod.NetProfit,Margin=previousPeriod.MarginPercent.HasValue?previousPeriod.MarginPercent.Value.ToString("N2")+"%":T("N/A","نامشخص")}};
  }
  rangeRefresh.Click+=(_,_)=>{try{RefreshRange();}catch(Exception ex){ShowOperationError(ex,"Compare periods","مقایسه دوره‌ها");}};
  rangeExport.Click+=(_,_)=>{try{
   RefreshRange();
   var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName="dashboard-period-comparison.csv"};
   if(dialog.ShowDialog()!=true)return;
   File.WriteAllText(dialog.FileName,ManagementAnalyticsService.ComparePeriodsCsv(selectedPeriod!,previousPeriod!),new UTF8Encoding(true));
   MessageBox.Show(T("Comparison exported.","مقایسه صادر شد."));
  }catch(Exception ex){ShowOperationError(ex,"Export comparison","خروجی مقایسه");}};
  RefreshRange();
  // Stage 87: drill down into posted revenue/expense accounts for the selected dates.
  p.Children.Add(Heading(T("Account contribution breakdown","سهم حساب‌ها از درآمد و هزینه")));
  p.Children.Add(Info(T("Posted entries only; negative reversals remain visible. No currency conversion.",
   "فقط اسناد قطعی؛ برگشت‌ها با علامت منفی نمایش داده می‌شوند. تبدیل ارز انجام نمی‌شود.")));
  var accountControls=new WrapPanel{Margin=new Thickness(0,6,0,8)};
  var accountType=new ComboBox{Width=180,Margin=new Thickness(0,0,10,6),
   ItemsSource=new[]{T("Revenue","درآمد"),T("Expense","هزینه")},SelectedIndex=0};
  var accountExport=new Button{Content=T("Export account CSV","خروجی CSV حساب‌ها"),Margin=new Thickness(0,0,10,6)};
  accountControls.Children.Add(accountType);accountControls.Children.Add(accountExport);p.Children.Add(accountControls);
  // Stage 88: accessible top-five account contribution chart (signed amounts, absolute share).
  var accountTopTitle=Heading(T("Top five accounts by contribution","پنج حساب برتر بر اساس سهم"));
  p.Children.Add(accountTopTitle);
  var accountTopChart=new StackPanel{Margin=new Thickness(0,4,0,14)};
  p.Children.Add(accountTopChart);
  var accountGrid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=true,Height=230,Margin=new Thickness(0,0,0,18)};
  p.Children.Add(accountGrid);
  IReadOnlyList<ManagementAnalyticsService.AccountContribution> accountRows=Array.Empty<ManagementAnalyticsService.AccountContribution>();
  void RefreshAccounts(){
   if(!LocalizedDateInput.TryParse(rangeFrom.Text,persian,out var from) ||
      !LocalizedDateInput.TryParse(rangeTo.Text,persian,out var to) || from>to)
    throw new ArgumentException(T("Invalid date range.","بازه تاریخ نامعتبر است."));
   var type=accountType.SelectedIndex==1?"Expense":"Revenue";
   accountRows=ManagementAnalyticsService.BreakdownByAccount(accounts,entries,from,to,type);
   accountTopChart.Children.Clear();
   var topFive=accountRows.Take(5).ToList();
   var absoluteTotal=accountRows.Sum(x=>Math.Abs(x.Amount));
   if(topFive.Count==0 || absoluteTotal==0m){
    accountTopChart.Children.Add(Info(T("No non-zero account contributions for this period.","در این بازه سهم غیرصفری برای حساب‌ها ثبت نشده است.")));
   }else{
    var maxAbsolute=topFive.Max(x=>Math.Abs(x.Amount));
    foreach(var item in topFive){
     var itemPanel=new StackPanel{Margin=new Thickness(0,0,0,10)};
     var share=Math.Round(Math.Abs(item.Amount)/absoluteTotal*100m,2);
     var caption=new TextBlock{
      Text=$"{item.AccountCode} — {item.AccountName}  |  {item.Amount:N2}  ({share:N2}%)",
      FontSize=12,TextWrapping=TextWrapping.Wrap,
      Foreground=(Brush)Application.Current.Resources["TextPrimary"]};
     itemPanel.Children.Add(caption);
     var bar=new Border{Height=13,HorizontalAlignment=HorizontalAlignment.Left,
      Width=Math.Max(3,Math.Round(330*(double)(Math.Abs(item.Amount)/maxAbsolute))),
      Background=(Brush)new BrushConverter().ConvertFromString(
       item.Amount<0m?(lightTheme?"#B45309":"#FBBF24"):
       (lightTheme?"#087E72":"#26D2B0"))!,
      CornerRadius=new CornerRadius(5),Margin=new Thickness(0,5,0,0),
      ToolTip=$"{item.AccountCode}: {item.Amount:N2} ({share:N2}%)"};
     itemPanel.Children.Add(bar);accountTopChart.Children.Add(itemPanel);
    }
    accountTopChart.Children.Add(Info(T("Share is based on absolute contributions of all accounts; signed amounts are preserved.",
     "درصد سهم از مجموع قدرمطلق مبالغ همه حساب‌ها محاسبه می‌شود؛ علامت مبالغ حفظ شده است.")));
   }
   accountGrid.ItemsSource=accountRows.Select(x=>new{
    Code=x.AccountCode,Name=x.AccountName,Type=T(type,type=="Revenue"?"درآمد":"هزینه"),Amount=x.Amount}).ToList();
  }
  accountType.SelectionChanged+=(_,_)=>{try{RefreshAccounts();}catch(Exception ex){ShowOperationError(ex,"Account breakdown","تحلیل حساب‌ها");}};
  rangeRefresh.Click+=(_,_)=>{try{RefreshAccounts();}catch(Exception ex){ShowOperationError(ex,"Account breakdown","تحلیل حساب‌ها");}};
  accountExport.Click+=(_,_)=>{try{
   RefreshAccounts();
   var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName="dashboard-account-breakdown.csv"};
   if(dialog.ShowDialog()!=true)return;
   File.WriteAllText(dialog.FileName,ManagementAnalyticsService.AccountBreakdownCsv(accountRows),new UTF8Encoding(true));
   MessageBox.Show(T("Account breakdown exported.","گزارش حساب‌ها صادر شد."));
  }catch(Exception ex){ShowOperationError(ex,"Account CSV","خروجی حساب‌ها");}};
  RefreshAccounts();
  // Stage 91: local budget presets; never writes accounting data.
  p.Children.Add(Heading(T("Expense budget check","کنترل بودجه هزینه‌ها")));
  p.Children.Add(Info(T("Select an expense account, enter a non-negative budget and compare it with posted expenses for the selected period. Budget presets can be saved locally without changing ledger data.",
   "حساب هزینه را انتخاب و بودجه غیرمنفی وارد کنید. مقایسه با هزینه‌های اسناد قطعی در بازه انتخابی انجام می‌شود. بودجه را می‌توان جدا از اطلاعات حسابداری در رایانه ذخیره کرد.")));
  var budgetAccounts=accounts.Where(a=>a.Type=="Expense").OrderBy(a=>a.Code).ToList();
  var budgetControls=new WrapPanel{Margin=new Thickness(0,6,0,10)};
  var budgetAccount=new ComboBox{Width=280,Margin=new Thickness(0,0,10,8),
   ItemsSource=budgetAccounts,DisplayMemberPath="Name",SelectedIndex=budgetAccounts.Count>0?0:-1};
  var budgetLimit=new TextBox{Width=170,Text="0",Margin=new Thickness(0,0,10,8),
   ToolTip=T("Budget limit (non-negative)","سقف بودجه (غیرمنفی)")};
  var budgetCheck=new Button{Content=T("Check budget","بررسی بودجه"),Margin=new Thickness(0,0,10,8)};
  var budgetExport=new Button{Content=T("Export budget CSV","خروجی CSV بودجه"),Margin=new Thickness(0,0,10,8)};
  budgetControls.Children.Add(budgetAccount);budgetControls.Children.Add(budgetLimit);
  budgetControls.Children.Add(budgetCheck);budgetControls.Children.Add(budgetExport);
  var budgetSave=new Button{Content=T("Save preset","ذخیره بودجه"),Margin=new Thickness(0,0,10,8)};
  var budgetLoad=new Button{Content=T("Load preset","بازیابی بودجه"),Margin=new Thickness(0,0,10,8)};
  var budgetDelete=new Button{Content=T("Delete preset","حذف بودجه"),Margin=new Thickness(0,0,10,8)};
  budgetControls.Children.Add(budgetSave);budgetControls.Children.Add(budgetLoad);budgetControls.Children.Add(budgetDelete);
  p.Children.Add(budgetControls);
  var budgetStore=new BudgetPresetStore();
  // Initialize all captured UI variables before subscribing event handlers (CS0165).
  var budgetCards=new WrapPanel{Margin=new Thickness(0,0,0,12)};p.Children.Add(budgetCards);
  var savedBudgets=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true,Height=175,Margin=new Thickness(0,0,0,14)};
  savedBudgets.Columns.Add(new DataGridTextColumn{Header=T("Account ID","شناسه حساب"),Binding=new System.Windows.Data.Binding("AccountId")});
  savedBudgets.Columns.Add(new DataGridTextColumn{Header=T("From","از تاریخ"),Binding=new System.Windows.Data.Binding("From") { StringFormat="yyyy-MM-dd" }});
  savedBudgets.Columns.Add(new DataGridTextColumn{Header=T("To","تا تاریخ"),Binding=new System.Windows.Data.Binding("To") { StringFormat="yyyy-MM-dd" }});
  savedBudgets.Columns.Add(new DataGridTextColumn{Header=T("Limit","سقف بودجه"),Binding=new System.Windows.Data.Binding("Limit") { StringFormat="N2" }});
  p.Children.Add(Heading(T("Saved budget presets","بودجه‌های ذخیره‌شده")));
  var budgetListRefresh=new Button{Content=T("Refresh saved budgets","به‌روزرسانی بودجه‌ها"),Margin=new Thickness(0,0,0,8)};
  p.Children.Add(budgetListRefresh);p.Children.Add(savedBudgets);
  void RefreshSavedBudgets()=>savedBudgets.ItemsSource=budgetStore.Load().OrderBy(x=>x.AccountId).ThenByDescending(x=>x.From).ToList();
  budgetListRefresh.Click+=(_,_)=>{try{RefreshSavedBudgets();}catch(Exception ex){ShowOperationError(ex,"List budgets","فهرست بودجه‌ها");}};
  try{RefreshSavedBudgets();}catch(Exception ex){ShowOperationError(ex,"List budgets","فهرست بودجه‌ها");}

  (Account Account,DateTime From,DateTime To) BudgetKey(){
   if(budgetAccount.SelectedItem is not Account selected || selected.Type!="Expense")
    throw new ArgumentException(T("Select an expense account.","یک حساب هزینه انتخاب کنید."));
   if(!LocalizedDateInput.TryParse(rangeFrom.Text,persian,out var from) ||
      !LocalizedDateInput.TryParse(rangeTo.Text,persian,out var to) || from>to)
    throw new ArgumentException(T("Invalid date range.","بازه تاریخ نامعتبر است."));
   return (selected,from.Date,to.Date);
  }
  budgetSave.Click+=(_,_)=>{try{
   var key=BudgetKey();
   if(!decimal.TryParse(budgetLimit.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var amount) &&
      !decimal.TryParse(budgetLimit.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out amount))
    throw new ArgumentException(T("Invalid budget amount.","مبلغ بودجه نامعتبر است."));
   budgetStore.Save(new BudgetPresetStore.Preset(key.Account.Id,key.From,key.To,amount));
   RefreshSavedBudgets();
   MessageBox.Show(T("Budget preset saved locally.","بودجه در رایانه ذخیره شد."));
  }catch(Exception ex){ShowOperationError(ex,"Save budget","ذخیره بودجه");}};
  budgetLoad.Click+=(_,_)=>{try{
   var key=BudgetKey();var saved=budgetStore.Load().FirstOrDefault(x=>x.AccountId==key.Account.Id && x.From==key.From && x.To==key.To);
   if(saved is null){MessageBox.Show(T("No saved preset for this account and period.","برای این حساب و بازه بودجه ذخیره‌شده‌ای وجود ندارد."));return;}
   budgetLimit.Text=saved.Limit.ToString(CultureInfo.CurrentCulture);
   RefreshBudget();
  }catch(Exception ex){ShowOperationError(ex,"Load budget","بازیابی بودجه");}};
  budgetDelete.Click+=(_,_)=>{try{
   var key=BudgetKey();
   if(MessageBox.Show(T("Delete the saved budget preset?","بودجه ذخیره‌شده حذف شود؟"),
      T("Confirm","تأیید"),MessageBoxButton.YesNo,MessageBoxImage.Question)!=MessageBoxResult.Yes)return;
   bool deleted=budgetStore.Delete(key.Account.Id,key.From,key.To);
   RefreshSavedBudgets();
   MessageBox.Show(deleted?T("Preset deleted.","بودجه حذف شد."):T("No saved preset found.","بودجه ذخیره‌شده پیدا نشد."));
  }catch(Exception ex){ShowOperationError(ex,"Delete budget","حذف بودجه");}};
  BudgetAnalyticsService.BudgetResult? budgetResult=null;DateTime budgetFrom=default,budgetTo=default;
  void RefreshBudget(){
   if(budgetAccount.SelectedItem is not Account selected)
    throw new InvalidOperationException(T("No expense account is available.","حساب هزینه‌ای موجود نیست."));
   if(!LocalizedDateInput.TryParse(rangeFrom.Text,persian,out var from) ||
      !LocalizedDateInput.TryParse(rangeTo.Text,persian,out var to) || from>to)
    throw new ArgumentException(T("Invalid date range.","بازه تاریخ نامعتبر است."));
   if(!decimal.TryParse(budgetLimit.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var limit) &&
      !decimal.TryParse(budgetLimit.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out limit))
    throw new ArgumentException(T("Enter a valid budget amount.","مبلغ بودجه معتبر وارد کنید."));
   budgetResult=BudgetAnalyticsService.Evaluate(selected,entries,from,to,limit);budgetFrom=from;budgetTo=to;
   budgetCards.Children.Clear();
   budgetCards.Children.Add(Stat(T("BUDGET","بودجه"),budgetResult.Limit.ToString("N2"),lightTheme?"#135C93":"#A8D8FF"));
   budgetCards.Children.Add(Stat(T("ACTUAL EXPENSE","هزینه واقعی"),budgetResult.Actual.ToString("N2"),lightTheme?"#925300":"#FFC178"));
   budgetCards.Children.Add(Stat(T("REMAINING","باقیمانده"),budgetResult.Remaining.ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
   budgetCards.Children.Add(Stat(T("BUDGET USED","درصد مصرف"),budgetResult.UsedPercent is decimal used ? used.ToString("N2")+"%" : T("N/A","نامشخص"),lightTheme?"#135C93":"#A8D8FF"));
   if(budgetResult.Exceeded)budgetCards.Children.Add(Info(T("⚠ Budget exceeded","⚠ بودجه تجاوز کرده است")));
  }
  budgetCheck.Click+=(_,_)=>{try{RefreshBudget();}catch(Exception ex){ShowOperationError(ex,"Budget check","بررسی بودجه");}};
  budgetExport.Click+=(_,_)=>{try{RefreshBudget();var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName="expense-budget.csv"};
   if(dialog.ShowDialog()!=true)return;
   File.WriteAllText(dialog.FileName,BudgetAnalyticsService.ToCsv(budgetResult!,budgetFrom,budgetTo),new UTF8Encoding(true));
   MessageBox.Show(T("Budget exported.","گزارش بودجه صادر شد."));
  }catch(Exception ex){ShowOperationError(ex,"Budget export","خروجی بودجه");}};
  // Stage 53: annual management analysis. Read-only, posted entries only.
  p.Children.Add(Heading(T("Annual management analysis","تحلیل مدیریتی سالانه")));
  p.Children.Add(Info(T("Revenue, expenses and net profit from posted journal entries. Months follow the Gregorian accounting dates; Persian labels do not change grouping.",
   "درآمد، هزینه و سود خالص از اسناد ثبت‌شده؛ گروه‌بندی ماهانه بر اساس تاریخ میلادی دفتر است و برچسب فارسی آن را تغییر نمی‌دهد.")));
  var annualControls=new WrapPanel{Margin=new Thickness(0,8,0,10)};
  var yearPicker=new ComboBox{Width=130,Margin=new Thickness(0,0,12,8),
   ItemsSource=Enumerable.Range(DateTime.Today.Year-9,11).Reverse().ToList(),SelectedItem=DateTime.Today.Year};
  var annualExport=new Button{Content=T("Export annual CSV","خروجی CSV سالانه"),Margin=new Thickness(0,0,12,8)};
  annualControls.Children.Add(yearPicker);annualControls.Children.Add(annualExport);p.Children.Add(annualControls);
  var annualTotals=new WrapPanel{Margin=new Thickness(0,0,0,8)};
  p.Children.Add(annualTotals);
  var annualGrid=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true,Height=340,Margin=new Thickness(0,0,0,20)};
  annualGrid.Columns.Add(new DataGridTextColumn{Header=T("Period (Gregorian)","دوره (میلادی)"),Binding=new System.Windows.Data.Binding("Period")});
  annualGrid.Columns.Add(new DataGridTextColumn{Header=T("Revenue","درآمد"),Binding=new System.Windows.Data.Binding("Revenue") { StringFormat="N2" }});
  annualGrid.Columns.Add(new DataGridTextColumn{Header=T("Expenses","هزینه"),Binding=new System.Windows.Data.Binding("Expenses") { StringFormat="N2" }});
  annualGrid.Columns.Add(new DataGridTextColumn{Header=T("Net profit","سود خالص"),Binding=new System.Windows.Data.Binding("NetProfit") { StringFormat="N2" }});
  p.Children.Add(annualGrid);
  IReadOnlyList<ManagementAnalyticsService.MonthSummary> AnnualRows()=>ManagementAnalyticsService.ForYear(accounts,entries,(int)(yearPicker.SelectedItem??DateTime.Today.Year));
  void RefreshAnnual(){
   var rows=AnnualRows();annualGrid.ItemsSource=rows;
   annualTotals.Children.Clear();
   annualTotals.Children.Add(Stat(T("ANNUAL REVENUE","درآمد سالانه"),rows.Sum(r=>r.Revenue).ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
   annualTotals.Children.Add(Stat(T("ANNUAL EXPENSES","هزینه سالانه"),rows.Sum(r=>r.Expenses).ToString("N2"),lightTheme?"#925300":"#FFC178"));
   annualTotals.Children.Add(Stat(T("ANNUAL NET PROFIT","سود خالص سالانه"),rows.Sum(r=>r.NetProfit).ToString("N2"),lightTheme?"#135C93":"#A8D8FF"));
  }
  yearPicker.SelectionChanged+=(_,_)=>RefreshAnnual();
  annualExport.Click+=(_,_)=>{
   try{
    var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName=$"management-{yearPicker.SelectedItem}.csv"};
    if(dialog.ShowDialog()!=true)return;
    File.WriteAllText(dialog.FileName,ManagementAnalyticsService.ToCsv(AnnualRows()),new UTF8Encoding(true));
    MessageBox.Show(T("Annual report exported.","گزارش سالانه صادر شد."));
   }catch(Exception ex){ShowOperationError(ex,"Annual report export","خطای خروجی گزارش سالانه");}
  };
  RefreshAnnual();
  // Stage 55: year-over-year comparison of posted accrual journal entries.
  p.Children.Add(Heading(T("Year-over-year profitability","مقایسه سودآوری دو سال متوالی")));
  p.Children.Add(Info(T("Posted revenue and expenses by Gregorian month. Percentage is N/A when previous profit is zero; negative prior profit uses absolute denominator.",
   "درآمد و هزینه اسناد ثبت‌شده به تفکیک ماه میلادی؛ درصد تغییر در صورت صفر بودن سود سال قبل محاسبه نمی‌شود.")));
  var yoyControls=new WrapPanel{Margin=new Thickness(0,8,0,10)};
  var yoyYear=new ComboBox{Width=130,Margin=new Thickness(0,0,12,8),ItemsSource=Enumerable.Range(DateTime.Today.Year-9,11).Reverse().ToList(),SelectedItem=DateTime.Today.Year};
  var yoyExport=new Button{Content=T("Export comparison CSV","خروجی CSV مقایسه"),Margin=new Thickness(0,0,12,8)};
  yoyControls.Children.Add(yoyYear);yoyControls.Children.Add(yoyExport);p.Children.Add(yoyControls);
  var yoyTotals=new WrapPanel{Margin=new Thickness(0,0,0,8)};p.Children.Add(yoyTotals);
  var yoyGrid=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true,Height=340,Margin=new Thickness(0,0,0,20)};
  yoyGrid.Columns.Add(new DataGridTextColumn{Header=T("Month","ماه"),Binding=new System.Windows.Data.Binding("MonthLabel")});
  yoyGrid.Columns.Add(new DataGridTextColumn{Header=T("Previous profit","سود سال قبل"),Binding=new System.Windows.Data.Binding("PreviousProfit"){StringFormat="N2"}});
  yoyGrid.Columns.Add(new DataGridTextColumn{Header=T("Current profit","سود سال جاری"),Binding=new System.Windows.Data.Binding("CurrentProfit"){StringFormat="N2"}});
  yoyGrid.Columns.Add(new DataGridTextColumn{Header=T("Profit change","تغییر سود"),Binding=new System.Windows.Data.Binding("ProfitChange"){StringFormat="N2"}});
  yoyGrid.Columns.Add(new DataGridTextColumn{Header=T("Change %","درصد تغییر"),Binding=new System.Windows.Data.Binding("ProfitChangePercent"){StringFormat="N2"}});
  p.Children.Add(yoyGrid);
  IReadOnlyList<YearOverYearService.Comparison> YoyRows()=>YearOverYearService.Compare(accounts,entries,(int)(yoyYear.SelectedItem??DateTime.Today.Year));
  void RefreshYoy(){
   var rows=YoyRows();yoyGrid.ItemsSource=rows;yoyTotals.Children.Clear();
   yoyTotals.Children.Add(Stat(T("PREVIOUS YEAR PROFIT","سود سال قبل"),rows.Sum(r=>r.PreviousProfit).ToString("N2"),lightTheme?"#135C93":"#A8D8FF"));
   yoyTotals.Children.Add(Stat(T("CURRENT YEAR PROFIT","سود سال جاری"),rows.Sum(r=>r.CurrentProfit).ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
   yoyTotals.Children.Add(Stat(T("YEAR-OVER-YEAR CHANGE","تغییر نسبت به سال قبل"),rows.Sum(r=>r.ProfitChange).ToString("N2"),lightTheme?"#925300":"#FFC178"));
  }
  yoyYear.SelectionChanged+=(_,_)=>RefreshYoy();
  yoyExport.Click+=(_,_)=>{
   try{
    var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName=$"year-over-year-{yoyYear.SelectedItem}.csv"};
    if(dialog.ShowDialog()!=true)return;
    File.WriteAllText(dialog.FileName,YearOverYearService.ToCsv(YoyRows()),new UTF8Encoding(true));
    MessageBox.Show(T("Comparison report exported.","گزارش مقایسه‌ای صادر شد."));
   }catch(Exception ex){ShowOperationError(ex,"Year-over-year export","خطای خروجی مقایسه سالانه");}
  };
  RefreshYoy();
  // Stage 54: treasury-based cash-flow report; deliberately separate from accrual net profit.
  p.Children.Add(Heading(T("Treasury cash flow — annual","جریان نقدینگی خزانه‌داری — سالانه")));
  p.Children.Add(Info(T("Recorded cash/bank receipts and payments only. This is not a cash-flow statement prepared under IFRS and excludes unrecorded bank movements.",
   "فقط دریافت‌ها و پرداخت‌های ثبت‌شده نقدی و بانکی؛ این گزارش صورت جریان وجوه نقد استاندارد IFRS نیست و تراکنش‌های ثبت‌نشده بانکی را شامل نمی‌شود.")));
  var cashControls=new WrapPanel{Margin=new Thickness(0,8,0,10)};
  var cashYear=new ComboBox{Width=130,Margin=new Thickness(0,0,12,8),ItemsSource=Enumerable.Range(DateTime.Today.Year-9,11).Reverse().ToList(),SelectedItem=DateTime.Today.Year};
  var cashExport=new Button{Content=T("Export cash flow CSV","خروجی CSV جریان نقدینگی"),Margin=new Thickness(0,0,12,8)};
  cashControls.Children.Add(cashYear);cashControls.Children.Add(cashExport);p.Children.Add(cashControls);
  var cashTotals=new WrapPanel{Margin=new Thickness(0,0,0,8)};p.Children.Add(cashTotals);
  var cashGrid=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true,Height=320,Margin=new Thickness(0,0,0,20)};
  cashGrid.Columns.Add(new DataGridTextColumn{Header=T("Period","دوره"),Binding=new System.Windows.Data.Binding("Period")});
  cashGrid.Columns.Add(new DataGridTextColumn{Header=T("Receipts","دریافت‌ها"),Binding=new System.Windows.Data.Binding("Receipts"){StringFormat="N2"}});
  cashGrid.Columns.Add(new DataGridTextColumn{Header=T("Payments","پرداخت‌ها"),Binding=new System.Windows.Data.Binding("Payments"){StringFormat="N2"}});
  cashGrid.Columns.Add(new DataGridTextColumn{Header=T("Net cash flow","خالص جریان نقد"),Binding=new System.Windows.Data.Binding("Net"){StringFormat="N2"}});
  cashGrid.Columns.Add(new DataGridTextColumn{Header=T("Cash receipts","دریافت نقدی"),Binding=new System.Windows.Data.Binding("CashReceipts"){StringFormat="N2"}});
  cashGrid.Columns.Add(new DataGridTextColumn{Header=T("Bank receipts","دریافت بانکی"),Binding=new System.Windows.Data.Binding("BankReceipts"){StringFormat="N2"}});
  p.Children.Add(cashGrid);
  IReadOnlyList<CashFlowAnalyticsService.MonthCashFlow> CashRows(){
   using var cashDb=new AshkanLedgerPro.Data.LedgerDb();
   var year=(int)(cashYear.SelectedItem??DateTime.Today.Year);
   var start=new DateTime(year,1,1);var end=start.AddYears(1);
   var tx=cashDb.TreasuryTransactions.AsNoTracking().Where(t=>t.OccurredAt>=start && t.OccurredAt<end).ToList();
   return CashFlowAnalyticsService.ForYear(tx,year);
  }
  void RefreshCash(){
   try{
    var rows=CashRows();cashGrid.ItemsSource=rows;cashTotals.Children.Clear();
    cashTotals.Children.Add(Stat(T("TOTAL RECEIPTS","جمع دریافت‌ها"),rows.Sum(r=>r.Receipts).ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
    cashTotals.Children.Add(Stat(T("TOTAL PAYMENTS","جمع پرداخت‌ها"),rows.Sum(r=>r.Payments).ToString("N2"),lightTheme?"#925300":"#FFC178"));
    cashTotals.Children.Add(Stat(T("NET CASH FLOW","خالص جریان نقد"),rows.Sum(r=>r.Net).ToString("N2"),lightTheme?"#135C93":"#A8D8FF"));
   }catch(Exception ex){ShowOperationError(ex,"Cash flow analysis","خطای تحلیل جریان نقدینگی");}
  }
  cashYear.SelectionChanged+=(_,_)=>RefreshCash();
  cashExport.Click+=(_,_)=>{
   try{
    var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName=$"cash-flow-{cashYear.SelectedItem}.csv"};
    if(dialog.ShowDialog()!=true)return;
    File.WriteAllText(dialog.FileName,CashFlowAnalyticsService.ToCsv(CashRows()),new UTF8Encoding(true));
    MessageBox.Show(T("Cash flow report exported.","گزارش جریان نقدینگی صادر شد."));
   }catch(Exception ex){ShowOperationError(ex,"Cash flow export","خطای خروجی جریان نقدینگی");}
  };
  RefreshCash();
  // Stage 89: visual movement trend and threshold alert. Cumulative movement is NOT a bank balance.
  p.Children.Add(Heading(T("Cash movement trend and threshold","روند جریان نقد و آستانه هشدار")));
  p.Children.Add(Info(T("Cumulative movement starts at zero and is NOT your available bank/cash balance. The threshold compares recorded movement only.",
   "جریان تجمعی از صفر شروع می‌شود و مانده واقعی صندوق یا بانک نیست. هشدار فقط با گردش ثبت‌شده مقایسه می‌شود.")));
  var trendControls=new WrapPanel{Margin=new Thickness(0,4,0,8)};
  trendControls.Children.Add(new TextBlock{Text=T("Warning threshold:","آستانه هشدار:"),VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(0,0,8,0)});
  var thresholdInput=new TextBox{Text="0",Width=145,Margin=new Thickness(0,0,10,0)};
  var trendRefresh=new Button{Content=T("Update trend","به‌روزرسانی روند"),Margin=new Thickness(0,0,10,0)};
  var trendExport=new Button{Content=T("Export trend CSV","خروجی CSV روند")};
  trendControls.Children.Add(thresholdInput);trendControls.Children.Add(trendRefresh);trendControls.Children.Add(trendExport);p.Children.Add(trendControls);
  var trendAlert=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,4,0,10),FontWeight=FontWeights.SemiBold};p.Children.Add(trendAlert);
  var trendChart=new StackPanel{Margin=new Thickness(0,0,0,18)};p.Children.Add(trendChart);
  IReadOnlyList<CashFlowAnalyticsService.LiquidityTrend> TrendRows(){
   if(!decimal.TryParse(thresholdInput.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var threshold) &&
      !decimal.TryParse(thresholdInput.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out threshold))
    throw new FormatException(T("Enter a valid numeric threshold.","آستانه عددی معتبر وارد کنید."));
   return CashFlowAnalyticsService.Trend(CashRows(),0m,threshold);
  }
  void RefreshTrend(){
   try{
    var rows=TrendRows();trendChart.Children.Clear();
    var max=rows.Count==0?1m:Math.Max(1m,rows.Max(x=>Math.Max(Math.Abs(x.Receipts),Math.Abs(x.Payments))));
    foreach(var r in rows){
     var line=new StackPanel{Margin=new Thickness(0,0,0,9)};
     line.Children.Add(new TextBlock{Text=$"{r.Month:00}   {T("Net","خالص")}: {r.Net:N2}   {T("Cumulative","تجمعی")}: {r.CumulativeNet:N2}",Foreground=Brushes.White});
     var bars=new StackPanel{Orientation=Orientation.Horizontal};
     bars.Children.Add(new Border{Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString("#26D2B0")),Width=(double)(r.Receipts/max*300m),Height=9,Margin=new Thickness(0,0,4,0)});
     bars.Children.Add(new Border{Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F2B36B")),Width=(double)(r.Payments/max*300m),Height=9});
     line.Children.Add(bars);trendChart.Children.Add(line);
    }
    var count=rows.Count(x=>x.BelowThreshold);
    trendAlert.Text=count>0?T($"Warning: {count} months below the movement threshold.",$"هشدار: {count} ماه کمتر از آستانه گردش ثبت‌شده است."):
     T("No months below the movement threshold.","هیچ ماهی کمتر از آستانه گردش ثبت‌شده نیست.");
    trendAlert.Foreground=new SolidColorBrush((Color)ColorConverter.ConvertFromString(count>0?"#F2B36B":"#26D2B0"));
   }catch(Exception ex){ShowOperationError(ex,"Cash movement trend","خطای روند گردش نقد");}
  }
  trendRefresh.Click+=(_,_)=>RefreshTrend();
  cashYear.SelectionChanged+=(_,_)=>RefreshTrend();
  trendExport.Click+=(_,_)=>{
   try{
    var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName=$"cash-trend-{cashYear.SelectedItem}.csv"};
    if(dialog.ShowDialog()!=true)return;
    File.WriteAllText(dialog.FileName,CashFlowAnalyticsService.TrendCsv(TrendRows()),new UTF8Encoding(true));
   }catch(Exception ex){ShowOperationError(ex,"Cash movement CSV","خطای خروجی روند نقد");}
  };
  RefreshTrend();
  // Stage 56: counterparty exposure, not invoice-level settled aging.
  p.Children.Add(Heading(T("Receivables / payables exposure","نمای کلی مطالبات و بدهی‌ها")));
  p.Children.Add(Info(T("Indicative partner balance = invoices minus recorded treasury receipts/payments. Age bands show ORIGINAL invoices, not unpaid invoice balances. Payments are not allocated to individual invoices; opening balances, credits and reversals are not reconciled here.",
    "مانده تقریبی هر شخص برابر فاکتورها منهای دریافت‌ها/پرداخت‌های ثبت‌شده است. بازه‌های سنی مربوط به مبلغ اولیه فاکتورها هستند، نه مانده تسویه‌نشده هر فاکتور. تخصیص پرداخت به فاکتور و مانده‌های افتتاحیه در این گزارش وجود ندارد.")));
  var exposureControls=new WrapPanel{Margin=new Thickness(0,8,0,10)};
  var exposureDate=new DatePicker{SelectedDate=DateTime.Today,Width=180,Margin=new Thickness(0,0,12,8)};
  var exposureRefresh=new Button{Content=T("Refresh exposure","به‌روزرسانی گزارش"),Margin=new Thickness(0,0,12,8)};
  var exposureExport=new Button{Content=T("Export exposure CSV","خروجی CSV مطالبات"),Margin=new Thickness(0,0,12,8)};
  exposureControls.Children.Add(exposureDate);exposureControls.Children.Add(exposureRefresh);exposureControls.Children.Add(exposureExport);
  p.Children.Add(exposureControls);
  var exposureTotals=new WrapPanel{Margin=new Thickness(0,0,0,8)};p.Children.Add(exposureTotals);
  var exposureGrid=new DataGrid{AutoGenerateColumns=false,IsReadOnly=true,Height=300,Margin=new Thickness(0,0,0,20)};
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Partner","شخص"),Binding=new System.Windows.Data.Binding("Partner")});
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Type","نوع"),Binding=new System.Windows.Data.Binding("Kind")});
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Invoices","فاکتورها"),Binding=new System.Windows.Data.Binding("Invoiced"){StringFormat="N2"}});
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Treasury settled","تسویه خزانه"),Binding=new System.Windows.Data.Binding("Settled"){StringFormat="N2"}});
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Indicative open","مانده تقریبی"),Binding=new System.Windows.Data.Binding("OpenBalance"){StringFormat="N2"}});
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Original invoices 0–30d","اصل فاکتور ۰–۳۰ روز"),Binding=new System.Windows.Data.Binding("Age0To30"){StringFormat="N2"}});
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Original invoices 31–60d","اصل فاکتور ۳۱–۶۰ روز"),Binding=new System.Windows.Data.Binding("Age31To60"){StringFormat="N2"}});
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Original invoices 61–90d","اصل فاکتور ۶۱–۹۰ روز"),Binding=new System.Windows.Data.Binding("Age61To90"){StringFormat="N2"}});
  exposureGrid.Columns.Add(new DataGridTextColumn{Header=T("Original invoices 90+d","اصل فاکتور بالای ۹۰ روز"),Binding=new System.Windows.Data.Binding("AgeOver90"){StringFormat="N2"}});
  p.Children.Add(exposureGrid);
  IReadOnlyList<PartnerExposureService.Exposure> ExposureRows(){
    using var exposureDb=new AshkanLedgerPro.Data.LedgerDb();
    return PartnerExposureService.Calculate(
      exposureDb.Partners.AsNoTracking().ToList(),
      exposureDb.SalesInvoices.AsNoTracking().ToList(),
      exposureDb.PurchaseInvoices.AsNoTracking().ToList(),
      exposureDb.TreasuryTransactions.AsNoTracking().ToList(),
      exposureDate.SelectedDate??DateTime.Today);
  }
  void RefreshExposure(){
    try{
      var rows=ExposureRows();exposureGrid.ItemsSource=rows;exposureTotals.Children.Clear();
      exposureTotals.Children.Add(Stat(T("CUSTOMER EXPOSURE","مانده تقریبی مشتریان"),rows.Where(r=>r.Kind=="Customer").Sum(r=>r.OpenBalance).ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
      exposureTotals.Children.Add(Stat(T("SUPPLIER EXPOSURE","مانده تقریبی تأمین‌کنندگان"),rows.Where(r=>r.Kind=="Supplier").Sum(r=>r.OpenBalance).ToString("N2"),lightTheme?"#925300":"#FFC178"));
    }catch(Exception ex){ShowOperationError(ex,"Exposure report","خطای گزارش مطالبات");}
  }
  exposureRefresh.Click+=(_,_)=>RefreshExposure();
  exposureExport.Click+=(_,_)=>{
    try{
      var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName=$"partner-exposure-{(exposureDate.SelectedDate??DateTime.Today):yyyyMMdd}.csv"};
      if(dialog.ShowDialog()!=true)return;
      File.WriteAllText(dialog.FileName,PartnerExposureService.ToCsv(ExposureRows()),new UTF8Encoding(true));
      MessageBox.Show(T("Exposure report exported.","گزارش مطالبات صادر شد."));
    }catch(Exception ex){ShowOperationError(ex,"Exposure export","خطای خروجی مطالبات");}
  };
  RefreshExposure();
  // Stage 57: explicit allocations, with invoice-level remaining balances.
  p.Children.Add(Heading(T("Invoice settlement allocations","تخصیص تسویه به فاکتور")));
  p.Children.Add(Info(T("Link an existing treasury transaction to an invoice of the same partner. No new accounting entry is posted. Allocations are immutable; verify before saving.",
    "یک تراکنش ثبت‌شده خزانه را به فاکتور همان شخص تخصیص دهید. سند حسابداری جدید صادر نمی‌شود. تخصیص‌ها قابل ویرایش یا حذف نیستند.")));
  var allocationControls=new WrapPanel{Margin=new Thickness(0,8,0,10)};
  var allocationKind=new ComboBox{Width=125,Margin=new Thickness(0,0,8,8),ItemsSource=new[]{"Sales","Purchase"},SelectedIndex=0};
  var allocationInvoice=new TextBox{Width=100,Margin=new Thickness(0,0,8,8),ToolTip=T("Invoice ID","شناسه فاکتور")};
  var allocationTreasury=new TextBox{Width=100,Margin=new Thickness(0,0,8,8),ToolTip=T("Treasury transaction ID","شناسه تراکنش خزانه")};
  var allocationAmount=new TextBox{Width=130,Margin=new Thickness(0,0,8,8),ToolTip=T("Allocation amount","مبلغ تخصیص")};
  var allocationSave=new Button{Content=T("Allocate payment","ثبت تخصیص"),Margin=new Thickness(0,0,8,8)};
  allocationControls.Children.Add(allocationKind);allocationControls.Children.Add(allocationInvoice);
  allocationControls.Children.Add(allocationTreasury);allocationControls.Children.Add(allocationAmount);allocationControls.Children.Add(allocationSave);
  p.Children.Add(allocationControls);
  var outstandingGrid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=true,Height=240,Margin=new Thickness(0,0,0,10)};
  p.Children.Add(outstandingGrid);
  var outstandingRefresh=new Button{Content=T("Refresh invoice balances","به‌روزرسانی مانده فاکتورها"),Margin=new Thickness(0,0,0,12)};
  p.Children.Add(outstandingRefresh);
  void RefreshOutstanding(){
    try{using var d=new AshkanLedgerPro.Data.LedgerDb();
      outstandingGrid.ItemsSource=InvoiceAllocationService.OutstandingInvoices(d,exposureDate.SelectedDate??DateTime.Today).ToList();
    }catch(Exception ex){ShowOperationError(ex,"Invoice allocations","خطای تخصیص فاکتور");}
  }
  outstandingRefresh.Click+=(_,_)=>RefreshOutstanding();
  allocationSave.Click+=(_,_)=>{
    try{
      if(!int.TryParse(allocationInvoice.Text,out var invoiceId)||!int.TryParse(allocationTreasury.Text,out var treasuryId)||
        !decimal.TryParse(allocationAmount.Text,System.Globalization.NumberStyles.Number,System.Globalization.CultureInfo.CurrentCulture,out var amount))
        throw new ArgumentException(T("Enter valid invoice ID, treasury ID and amount.","شناسه فاکتور، شناسه خزانه و مبلغ معتبر وارد کنید."));
      using var d=new AshkanLedgerPro.Data.LedgerDb();
      InvoiceAllocationService.Allocate(d,treasuryId,(string)allocationKind.SelectedItem,invoiceId,amount);
      RefreshOutstanding();
      MessageBox.Show(T("Allocation saved.","تخصیص ثبت شد."));
    }catch(Exception ex){ShowOperationError(ex,"Invoice allocation","خطای ثبت تخصیص");}
  };
  // Stage 61: controlled reversal keeps the original allocation and an immutable audit row.
  p.Children.Add(Heading(T("Reverse an allocation", "برگشت کنترل‌شده تخصیص")));
  p.Children.Add(Info(T("Reversal releases the invoice and treasury balances without deleting the original allocation. Provide a reason and verify the allocation ID.",
    "برگشت، مانده فاکتور و خزانه را آزاد می‌کند بدون حذف تخصیص اصلی. شناسه تخصیص و دلیل اصلاح را بررسی کنید.")));
  var reversalPanel = new WrapPanel { Margin = new Thickness(0,8,0,10) };
  var reversalId = new TextBox { Width = 115, Margin = new Thickness(0,0,8,8), ToolTip = T("Allocation ID", "شناسه تخصیص") };
  var reversalReason = new TextBox { Width = 330, Margin = new Thickness(0,0,8,8), ToolTip = T("Reason (at least 8 characters)", "دلیل برگشت (حداقل ۸ نویسه)") };
  var reversalButton = new Button { Content = T("Reverse allocation", "برگشت تخصیص"), Margin = new Thickness(0,0,8,8) };
  var reversalHistoryButton = new Button { Content = T("Reversal audit", "تاریخچه برگشت‌ها"), Margin = new Thickness(0,0,8,8) };
  reversalPanel.Children.Add(reversalId); reversalPanel.Children.Add(reversalReason);
  reversalPanel.Children.Add(reversalButton); reversalPanel.Children.Add(reversalHistoryButton);
  p.Children.Add(reversalPanel);
  reversalButton.Click += (_,_) => {
    try {
      if (!int.TryParse(reversalId.Text,out var id)) throw new ArgumentException(T("Invalid allocation ID.","شناسه تخصیص نامعتبر است."));
      if (MessageBox.Show(T("Reverse this allocation? The action cannot be undone.","این تخصیص برگشت داده شود؟ این عملیات قابل بازگشت نیست."),
        T("Confirm reversal","تأیید برگشت"),MessageBoxButton.YesNo,MessageBoxImage.Warning)!=MessageBoxResult.Yes) return;
      using var d = new AshkanLedgerPro.Data.LedgerDb();
      InvoiceAllocationService.Reverse(d,id,reversalReason.Text,currentUser.Username);
      RefreshOutstanding();
      MessageBox.Show(T("Allocation reversed and audited.","تخصیص برگشت داده شد و در تاریخچه ثبت شد."));
    } catch(Exception ex) { ShowOperationError(ex,"Allocation reversal","خطای برگشت تخصیص"); }
  };
  reversalHistoryButton.Click += (_,_) => {
    try {
      using var d = new AshkanLedgerPro.Data.LedgerDb();
      var window = new Window { Title = T("Allocation reversal audit","تاریخچه برگشت تخصیص"), Width = 900, Height = 520,
        Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
      window.Content = new DataGrid { IsReadOnly = true, AutoGenerateColumns = true,
        ItemsSource = InvoiceAllocationService.ReversalHistory(d).ToList(), Margin = new Thickness(12) };
      window.ShowDialog();
    } catch(Exception ex) { ShowOperationError(ex,"Reversal history","خطای تاریخچه برگشت"); }
  };
  var allocationAuditButton = new Button { Content = T("Allocation audit & CSV", "حسابرسی تخصیص‌ها و CSV"), Margin = new Thickness(0,0,8,8) };
  reversalPanel.Children.Add(allocationAuditButton);
  allocationAuditButton.Click += (_,_) => {
    try {
      using var auditDb = new AshkanLedgerPro.Data.LedgerDb();
      var auditRows = InvoiceAllocationService.AllocationAudit(auditDb).ToList();
      var auditWindow = new Window { Title = T("Allocation audit", "گزارش حسابرسی تخصیص‌ها"),
        Width = 1100, Height = 620, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
      var auditPanel = new DockPanel { Margin = new Thickness(12) };
      var exportButton = new Button { Content = T("Export audit CSV", "خروجی CSV حسابرسی"),
        Margin = new Thickness(0,0,0,8), Padding = new Thickness(12,6,12,6) };
      DockPanel.SetDock(exportButton,Dock.Top);
      auditPanel.Children.Add(exportButton);
      auditPanel.Children.Add(new DataGrid { IsReadOnly = true, AutoGenerateColumns = true, ItemsSource = auditRows });
      exportButton.Click += (_,_) => {
        try {
          var dialog = new SaveFileDialog { Filter = "CSV files (*.csv)|*.csv", FileName = "allocation-audit.csv" };
          if (dialog.ShowDialog(auditWindow) != true) return;
          string CsvField(string? value) => "\"" + (value ?? "").Replace("\"","\"\"") + "\"";
          var lines = new List<string> { "AllocationId,InvoiceKind,InvoiceId,TreasuryTransactionId,Amount,AllocatedAtUtc,Status,ReversedAtUtc,ReversedBy,ReversalReason" };
          foreach (var row in auditRows)
            lines.Add(string.Join(",",new[] { row.AllocationId.ToString(CultureInfo.InvariantCulture), CsvField(row.InvoiceKind),
              row.InvoiceId.ToString(CultureInfo.InvariantCulture),row.TreasuryTransactionId.ToString(CultureInfo.InvariantCulture),
              row.Amount.ToString(CultureInfo.InvariantCulture),CsvField(row.AllocatedAtUtc),CsvField(row.Status),
              CsvField(row.ReversedAtUtc),CsvField(row.ReversedBy),CsvField(row.ReversalReason) }));
          File.WriteAllLines(dialog.FileName,lines,new UTF8Encoding(true));
          MessageBox.Show(T("Audit CSV exported.","خروجی حسابرسی ذخیره شد."));
        } catch(Exception ex) { ShowOperationError(ex,"Audit export","خطای خروجی حسابرسی"); }
      };
      auditWindow.Content = auditPanel;
      auditWindow.ShowDialog();
    } catch(Exception ex) { ShowOperationError(ex,"Allocation audit","خطای گزارش حسابرسی تخصیص‌ها"); }
  };
  // Stage 63: historical allocation snapshots and filtered audit XLSX.
  var historicalButton = new Button { Content = T("Historical balances & XLSX", "مانده تاریخی و خروجی اکسل"), Margin = new Thickness(0,0,8,8) };
  reversalPanel.Children.Add(historicalButton);
  historicalButton.Click += (_,_) => {
    try {
      var historicalWindow = new Window { Title = T("Historical invoice balances", "مانده تاریخی فاکتورها"),
        Width = 1100, Height = 680, Owner = this, WindowStartupLocation = WindowStartupLocation.CenterOwner };
      var panel = new DockPanel { Margin = new Thickness(12) };
      var controls = new WrapPanel { Margin = new Thickness(0,0,0,10) };
      var fromDate = new DatePicker { SelectedDate = DateTime.Today.AddMonths(-3), Width = 145, Margin = new Thickness(0,0,8,8),
        ToolTip = T("Invoice date from", "تاریخ شروع فاکتور") };
      var date = new DatePicker { SelectedDate = DateTime.Today, Width = 145, Margin = new Thickness(0,0,8,8),
        ToolTip = T("As-of date / invoice date to", "تاریخ گزارش و پایان بازه فاکتورها") };
      var kind = new ComboBox { Width = 140, ItemsSource = new[] { "All", "Sales", "Purchase" }, SelectedIndex = 0, Margin = new Thickness(0,0,8,8) };
      var refresh = new Button { Content = T("Refresh snapshot", "بروزرسانی گزارش"), Margin = new Thickness(0,0,8,8) };
      var excel = new Button { Content = T("Export XLSX", "خروجی XLSX"), Margin = new Thickness(0,0,8,8) };
      var pdfExport = new Button { Content = T("PDF + SHA-256", "PDF + رسید صحت"), Margin = new Thickness(0,0,8,8) };
      var partnerFilter = new TextBox { Width = 120, Margin = new Thickness(0,0,8,8),
        ToolTip = T("Partner ID (blank = all)", "شناسه طرف حساب (خالی = همه)") };
      var outstandingOnly = new CheckBox { Content = T("Unsettled only", "فقط تسویه‌نشده"),
        VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0,0,12,8) };
      controls.Children.Add(fromDate); controls.Children.Add(date); controls.Children.Add(kind); controls.Children.Add(partnerFilter);
      var verifyPdf = new Button { Content = T("Verify PDF receipt", "بررسی صحت PDF"), Margin = new Thickness(0,0,8,8) };
      var printReport = new Button { Content = T("Print report", "چاپ گزارش"), Margin = new Thickness(0,0,8,8) };
      var persianPdf = new Button { Content = T("Persian PDF (Windows)", "PDF فارسی (ویندوز)"), Margin = new Thickness(0,0,8,8), ToolTip = T("Choose Microsoft Print to PDF in the printer dialog.", "در پنجره چاپ، Microsoft Print to PDF را انتخاب کنید.") };
      controls.Children.Add(outstandingOnly); controls.Children.Add(refresh); controls.Children.Add(excel); controls.Children.Add(pdfExport); controls.Children.Add(verifyPdf); controls.Children.Add(printReport); controls.Children.Add(persianPdf);
      DockPanel.SetDock(controls,Dock.Top); panel.Children.Add(controls);
      // Stage 64: compact, theme-aware KPI ribbon for historical receivables and payables.
      var kpiRibbon = new UniformGrid { Columns = 4, Margin = new Thickness(0,0,0,12) };
      var kpiCards = new TextBlock[4];
      for (int i = 0; i < kpiCards.Length; i++) {
        var frame = new Border { Padding = new Thickness(14), Margin = new Thickness(0,0,8,0),
          CornerRadius = new CornerRadius(12), Background = (Brush)Application.Current.Resources["StatPanel"] };
        kpiCards[i] = new TextBlock { FontSize = 15, FontWeight = FontWeights.SemiBold,
          Foreground = (Brush)Application.Current.Resources["TextPrimary"], TextWrapping = TextWrapping.Wrap };
        frame.Child = kpiCards[i]; kpiRibbon.Children.Add(frame);
      }
      DockPanel.SetDock(kpiRibbon,Dock.Top); panel.Children.Add(kpiRibbon);
      // Stage 65: compact proportional bars, driven by the same filtered rows as the KPI cards.
      var chartPanel = new StackPanel { Margin = new Thickness(0,0,0,12) };
      var chartRows = new StackPanel[3];
      var chartValues = new TextBlock[3];
      var chartBars = new ProgressBar[3];
      string[] chartEn = { "Receivables", "Payables", "Allocated" };
      string[] chartFa = { "مطالبات", "بدهی‌ها", "تخصیص‌یافته" };
      for (int i=0; i<3; i++) {
        chartRows[i] = new StackPanel { Margin = new Thickness(0,0,0,6) };
        chartValues[i] = new TextBlock { Foreground = (Brush)Application.Current.Resources["TextPrimary"] };
        chartBars[i] = new ProgressBar { Height = 12, Minimum = 0, Maximum = 100, Margin = new Thickness(0,3,0,0) };
        chartRows[i].Children.Add(chartValues[i]); chartRows[i].Children.Add(chartBars[i]); chartPanel.Children.Add(chartRows[i]);
      }
      DockPanel.SetDock(chartPanel,Dock.Top); panel.Children.Add(chartPanel);
      // Stage 66: a native WPF monthly trend chart; no external plotting dependencies.
      var trendHeading = new TextBlock { Text = T("Monthly invoice totals (filtered snapshot)", "روند ماهانه مبلغ فاکتورها (با فیلتر جاری)"),
        FontWeight = FontWeights.SemiBold, Margin = new Thickness(0,8,0,4),
        Foreground = (Brush)Application.Current.Resources["TextPrimary"] };
      chartPanel.Children.Add(trendHeading);
      var trendCanvas = new Canvas { Height = 125, MinWidth = 250, Background = Brushes.Transparent, ClipToBounds = true };
      chartPanel.Children.Add(trendCanvas);
      var trendLegend = new TextBlock { Text = T("Blue: sales  |  Amber: purchases", "آبی: فروش  |  نارنجی: خرید"),
        FontSize = 11, Foreground = (Brush)Application.Current.Resources["TextPrimary"] };
      chartPanel.Children.Add(trendLegend);
      void DrawTrend(IReadOnlyList<InvoiceAllocationService.HistoricalInvoiceBalance> items) {
        trendCanvas.Children.Clear();
        var months = items.Select(x => new DateTime(x.InvoiceDateUtc.Year, x.InvoiceDateUtc.Month, 1))
          .Distinct().OrderBy(x => x).ToList();
        if (months.Count == 0) { trendLegend.Text = T("No invoices for the selected filters.", "فاکتوری با فیلترهای انتخابی یافت نشد."); return; }
        trendLegend.Text = T("Blue: sales  |  Amber: purchases", "آبی: فروش  |  نارنجی: خرید") +
          "    " + months.First().ToString("yyyy-MM") + " — " + months.Last().ToString("yyyy-MM");
        var series = new[] { "Sales", "Purchase" };
        var colors = new Brush[] { Brushes.DodgerBlue, Brushes.DarkOrange };
        var totals = series.Select(type => months.Select(month =>
          items.Where(x => x.InvoiceKind == type && x.InvoiceDateUtc.Year == month.Year && x.InvoiceDateUtc.Month == month.Month)
            .Sum(x => x.InvoiceTotal)).ToArray()).ToArray();
        var max = Math.Max(1m, totals.SelectMany(x => x).DefaultIfEmpty(0m).Max());
        double width = Math.Max(250, trendCanvas.ActualWidth > 0 ? trendCanvas.ActualWidth : 600);
        const double height = 105, pad = 12;
        for (int i=0; i<series.Length; i++) {
          var line = new System.Windows.Shapes.Polyline { Stroke = colors[i], StrokeThickness = 2.5,
            StrokeLineJoin = PenLineJoin.Round };
          for (int j=0; j<months.Count; j++) {
            double x = pad + (months.Count == 1 ? (width - 2*pad)/2 : j * (width - 2*pad)/(months.Count-1));
            double y = pad + (height - 2*pad) * (1 - (double)(totals[i][j] / max));
            line.Points.Add(new Point(x,y));
            var dot = new System.Windows.Shapes.Ellipse { Width = 6, Height = 6, Fill = colors[i],
              ToolTip = months[j].ToString("yyyy-MM") + " — " + totals[i][j].ToString("N2") };
            Canvas.SetLeft(dot,x-3); Canvas.SetTop(dot,y-3); trendCanvas.Children.Add(dot);
          }
          trendCanvas.Children.Add(line);
        }
      }

      var grid = new DataGrid { IsReadOnly = true, AutoGenerateColumns = true, AlternationCount = 2,
        CanUserAddRows = false, CanUserDeleteRows = false, GridLinesVisibility = DataGridGridLinesVisibility.Horizontal };
      panel.Children.Add(grid);
      List<InvoiceAllocationService.HistoricalInvoiceBalance> visible = new();
      trendCanvas.SizeChanged += (_,_) => DrawTrend(visible);
      void Load() {
        if (date.SelectedDate is null) throw new InvalidOperationException(T("Select a date.","تاریخ را انتخاب کنید."));
        if (fromDate.SelectedDate is null || fromDate.SelectedDate.Value.Date > date.SelectedDate.Value.Date)
          throw new InvalidOperationException(T("Choose a valid invoice date range.", "بازه تاریخ فاکتورها معتبر نیست."));
        var fromUtc = DateTime.SpecifyKind(fromDate.SelectedDate.Value.Date, DateTimeKind.Local).ToUniversalTime();
        // End of selected local calendar day converted to UTC; future allocations are excluded.
        var endOfDayUtc = DateTime.SpecifyKind(date.SelectedDate.Value.Date.AddDays(1).AddTicks(-1), DateTimeKind.Local).ToUniversalTime();
        using var d = new AshkanLedgerPro.Data.LedgerDb();
        var rows = InvoiceAllocationService.HistoricalBalances(d,endOfDayUtc);
        if (!string.IsNullOrWhiteSpace(partnerFilter.Text) &&
            (!int.TryParse(partnerFilter.Text, NumberStyles.None, CultureInfo.InvariantCulture, out var _) ||
             !int.TryParse(partnerFilter.Text, out var partnerId) || partnerId <= 0))
          throw new InvalidOperationException(T("Partner ID must be a positive integer.", "شناسه طرف حساب باید عدد مثبت باشد."));
        visible = rows.Where(x => x.InvoiceDateUtc >= fromUtc &&
          ((string?)kind.SelectedItem == "All" || x.InvoiceKind == (string?)kind.SelectedItem)
          && (string.IsNullOrWhiteSpace(partnerFilter.Text) || x.PartnerId.ToString(CultureInfo.InvariantCulture) == partnerFilter.Text.Trim())
          && (outstandingOnly.IsChecked != true || x.RemainingAsOf > 0)).ToList();
        grid.ItemsSource = visible;
        DrawTrend(visible);
        var receivables = visible.Where(x => x.InvoiceKind == "Sales").Sum(x => x.RemainingAsOf);
        var payables = visible.Where(x => x.InvoiceKind == "Purchase").Sum(x => x.RemainingAsOf);
        var settled = visible.Sum(x => x.AllocatedAsOf);
        kpiCards[0].Text = T("Receivables", "مطالبات") + "\n" + receivables.ToString("N2");
        kpiCards[1].Text = T("Payables", "بدهی‌ها") + "\n" + payables.ToString("N2");
        kpiCards[2].Text = T("Allocated", "تسویه‌شده") + "\n" + settled.ToString("N2");
        kpiCards[3].Text = T("Invoices", "فاکتورها") + "\n" + visible.Count.ToString("N0");
        decimal[] values = { receivables, payables, settled };
        var maximum = Math.Max(1m, values.Max());
        for (int i=0; i<values.Length; i++) {
          chartValues[i].Text = T(chartEn[i],chartFa[i]) + ": " + values[i].ToString("N2");
          chartBars[i].Value = (double)(Math.Max(0m,values[i]) / maximum * 100m);
        }
      }
      refresh.Click += (_,_) => { try { Load(); } catch(Exception ex) { ShowOperationError(ex,"Historical balances","خطای مانده تاریخی"); } };
      excel.Click += (_,_) => {
        try {
          Load();
          var dialog = new SaveFileDialog { Filter = "Excel workbook (*.xlsx)|*.xlsx", FileName = "historical-invoice-balances.xlsx" };
          if (dialog.ShowDialog(historicalWindow) != true) return;
          XlsxExportService.SaveTyped(dialog.FileName,
            new[] { "InvoiceKind","InvoiceId","PartnerId","InvoiceDateUtc","InvoiceTotal","AllocatedAsOf","RemainingAsOf" },
            visible.Select(x => (IReadOnlyList<object?>)new object?[] { x.InvoiceKind,x.InvoiceId.ToString(CultureInfo.InvariantCulture),
              x.PartnerId.ToString(CultureInfo.InvariantCulture),x.InvoiceDateUtc.ToString("O",CultureInfo.InvariantCulture),
              x.InvoiceTotal,x.AllocatedAsOf,x.RemainingAsOf }));
          MessageBox.Show(T("Historical XLSX exported.","فایل اکسل مانده تاریخی ذخیره شد."));
        } catch(Exception ex) { ShowOperationError(ex,"Historical XLSX","خطای خروجی اکسل"); }
      };
      pdfExport.Click += (_,_) => {
        try {
          Load();
          var dialog = new SaveFileDialog { Filter = "PDF document (*.pdf)|*.pdf", DefaultExt = ".pdf",
            AddExtension = true, FileName = "historical-invoice-balances.pdf" };
          if (dialog.ShowDialog(historicalWindow) != true) return;
          var checksumReceipt = IndependentPdfReportService.SaveHistoricalWithChecksum(dialog.FileName, fromDate.SelectedDate!.Value,
            date.SelectedDate!.Value, visible.Select(x => new IndependentPdfReportService.Row(
              x.InvoiceKind, x.InvoiceId, x.PartnerId, x.InvoiceTotal, x.AllocatedAsOf, x.RemainingAsOf)));
          MessageBox.Show(T("PDF and SHA-256 audit receipt saved (English/ASCII PDF).",
            "فایل PDF و رسید صحت‌سنجی SHA-256 ذخیره شدند (متن PDF انگلیسی است).") + "\n" + checksumReceipt);
        } catch (Exception ex) { ShowOperationError(ex,"Export PDF","خطای خروجی PDF"); }
      };
      verifyPdf.Click += (_,_) => {
        try {
          var dialog = new OpenFileDialog { Filter = "PDF document (*.pdf)|*.pdf", CheckFileExists = true };
          if (dialog.ShowDialog(historicalWindow) != true) return;
          var valid = IndependentPdfReportService.VerifyChecksum(dialog.FileName);
          MessageBox.Show(historicalWindow, valid
            ? T("SHA-256 matches the receipt. This does not prove who created the report.",
                "هش SHA-256 با رسید مطابقت دارد؛ این بررسی هویت صادرکننده را اثبات نمی‌کند.")
            : T("WARNING: The PDF differs from its SHA-256 receipt.",
                "هشدار: محتوای PDF با رسید SHA-256 مطابقت ندارد."),
            T("PDF integrity verification", "بررسی صحت PDF"), MessageBoxButton.OK,
            valid ? MessageBoxImage.Information : MessageBoxImage.Warning);
        } catch (Exception ex) { ShowOperationError(ex, "Verify PDF receipt", "خطای بررسی صحت PDF"); }
      };
      void PrintHistoricalReport(bool forcePersian) {
        try {
          Load();
          var print = new PrintDialog();
          if (forcePersian) {
            MessageBox.Show(historicalWindow,
              "برای ذخیره PDF فارسی، در پنجره بعدی چاپگر Microsoft Print to PDF را انتخاب کنید.\nاین روش از موتور متن Unicode ویندوز استفاده می‌کند و فایل SHA-256 جداگانه ایجاد نمی‌کند.",
              "خروجی PDF فارسی", MessageBoxButton.OK, MessageBoxImage.Information);
          }
          // To save a PDF, select the Windows "Microsoft Print to PDF" printer.
          if (print.ShowDialog() != true) return;
          var document = new FlowDocument { PagePadding = new Thickness(46, 52, 46, 62), ColumnWidth = double.PositiveInfinity,
            FontFamily = new FontFamily("Segoe UI"), FontSize = 10.5,
            FlowDirection = (forcePersian || LanguagePreferences.IsPersian()) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
          document.Blocks.Add(new Paragraph(new Run((forcePersian ? "گزارش مانده تاریخی فاکتورها" : T("Historical invoice balances", "گزارش مانده تاریخی فاکتورها"))))
            { FontSize = 18, FontWeight = FontWeights.Bold });
          document.Blocks.Add(new Paragraph(new Run("ASHKAN LEDGER PRO  /  FINANCIAL REPORT"))
            { FontSize = 9, Foreground = Brushes.SteelBlue, FontWeight = FontWeights.SemiBold,
              BorderBrush = Brushes.LightSteelBlue, BorderThickness = new Thickness(0,0,0,1),
              Padding = new Thickness(0,0,0,8) });
          document.Blocks.Add(new Paragraph(new Run((forcePersian ? "زمان تهیه (UTC)" : T("Generated (UTC)", "زمان تهیه (UTC)")) + ": " +
            DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))));
          document.Blocks.Add(new Paragraph(new Run((forcePersian ? "بازه" : T("Period", "بازه")) + ": " +
            fromDate.SelectedDate!.Value.ToString("yyyy-MM-dd") + " — " + date.SelectedDate!.Value.ToString("yyyy-MM-dd"))));
          var reportTable = new System.Windows.Documents.Table { CellSpacing = 0,
            FlowDirection = (forcePersian || LanguagePreferences.IsPersian()) ? FlowDirection.RightToLeft : FlowDirection.LeftToRight };
          for (int i=0;i<6;i++) reportTable.Columns.Add(new TableColumn());
          var group = new TableRowGroup(); reportTable.RowGroups.Add(group);
          void AddPrintRow(bool header, params string[] cells) {
            var row = new System.Windows.Documents.TableRow {
              Background = header ? new SolidColorBrush(Color.FromRgb(24, 56, 85)) :
                (group.Rows.Count % 2 == 0 ? Brushes.AliceBlue : Brushes.White)
            };
            foreach (var cell in cells) {
              var paragraph = new Paragraph(new Run(cell)) { Margin = new Thickness(0),
                Foreground = header ? Brushes.White : Brushes.Black,
                FontWeight = header ? FontWeights.Bold : FontWeights.Normal };
              row.Cells.Add(new TableCell(paragraph) {
                Padding = new Thickness(5, 7, 5, 7),
                BorderBrush = Brushes.LightGray,
                BorderThickness = new Thickness(0,0,0,0.5)
              });
            }
            group.Rows.Add(row);
          }
          AddPrintRow(true, (forcePersian ? "نوع" : T("Kind","نوع")),(forcePersian ? "فاکتور" : T("Invoice","فاکتور")),(forcePersian ? "طرف حساب" : T("Partner","طرف حساب")),
            (forcePersian ? "مبلغ کل" : T("Total","مبلغ کل")),(forcePersian ? "تخصیص" : T("Allocated","تخصیص")),(forcePersian ? "مانده" : T("Remaining","مانده")));
          foreach (var x in visible) AddPrintRow(false, (forcePersian ? (x.InvoiceKind == "Sales" ? "فروش" : "خرید") : T(x.InvoiceKind == "Sales" ? "Sales" : "Purchase",
            x.InvoiceKind == "Sales" ? "فروش" : "خرید")),x.InvoiceId.ToString(),x.PartnerId.ToString(),
            x.InvoiceTotal.ToString("N2"),x.AllocatedAsOf.ToString("N2"),x.RemainingAsOf.ToString("N2"));
          document.Blocks.Add(reportTable);
          document.Blocks.Add(new Paragraph(new Run((forcePersian ? "تعداد فاکتورها" : T("Invoice count", "تعداد فاکتورها")) + ": " + visible.Count +
            "    " + (forcePersian ? "جمع مانده" : T("Total remaining", "جمع مانده")) + ": " + visible.Sum(x => x.RemainingAsOf).ToString("N2")))
            { FontWeight = FontWeights.Bold });
          var paginator = ((IDocumentPaginatorSource)document).DocumentPaginator;
          paginator.PageSize = new Size(print.PrintableAreaWidth, print.PrintableAreaHeight);
          var numbered = new Services.ReportPageNumberPaginator(paginator,
            forcePersian || LanguagePreferences.IsPersian());
          print.PrintDocument(numbered, (forcePersian ? "مانده تاریخی" : T("Historical balances", "مانده تاریخی")));
        } catch(Exception ex) { ShowOperationError(ex,"Print historical report","خطای چاپ گزارش"); }
      }
      printReport.Click += (_,_) => PrintHistoricalReport(false);
      persianPdf.Click += (_,_) => PrintHistoricalReport(true);
      historicalWindow.Content = panel; Load(); historicalWindow.ShowDialog();
    } catch(Exception ex) { ShowOperationError(ex,"Historical balances","خطای مانده تاریخی"); }
  };
  RefreshOutstanding();
  // Stage 58: contractual due dates and invoice-level overdue aging.
  p.Children.Add(Heading(T("Contractual due date", "تاریخ سررسید قراردادی")));
  var duePanel=new WrapPanel{Margin=new Thickness(0,8,0,12)};
  var dueKind=new ComboBox{Width=125,Margin=new Thickness(0,0,8,8),ItemsSource=new[]{"Sales","Purchase"},SelectedIndex=0};
  var dueInvoice=new TextBox{Width=110,Margin=new Thickness(0,0,8,8),ToolTip=T("Invoice ID","شناسه فاکتور")};
  var dueDatePicker=new DatePicker{Width=155,Margin=new Thickness(0,0,8,8),SelectedDate=DateTime.Today.AddDays(30)};
  var dueSave=new Button{Content=T("Save due date","ثبت تاریخ سررسید"),Margin=new Thickness(0,0,8,8)};
  duePanel.Children.Add(dueKind);duePanel.Children.Add(dueInvoice);duePanel.Children.Add(dueDatePicker);duePanel.Children.Add(dueSave);
  p.Children.Add(duePanel);
  p.Children.Add(Info(T("Overdue aging is calculated only for invoices with a saved due date; missing due dates are explicitly marked.",
    "گزارش معوقات فقط برای فاکتورهای دارای تاریخ سررسید محاسبه می‌شود؛ تاریخ‌های نامشخص جداگانه مشخص می‌شوند.")));
  dueSave.Click+=(_,_)=>{
    try{
      if(!int.TryParse(dueInvoice.Text,out var invoiceId)||invoiceId<=0||dueDatePicker.SelectedDate is null)
        throw new ArgumentException(T("Enter a valid invoice ID and due date.","شناسه فاکتور و تاریخ سررسید معتبر وارد کنید."));
      using var d=new AshkanLedgerPro.Data.LedgerDb();
      InvoiceAllocationService.SetDueDate(d,(string)dueKind.SelectedItem,invoiceId,dueDatePicker.SelectedDate.Value,currentUser.Username);
      RefreshOutstanding();
      MessageBox.Show(T("Due date saved.","تاریخ سررسید ذخیره شد."));
    }catch(Exception ex){ShowOperationError(ex,"Invoice due date","خطای تاریخ سررسید");}
  };

  // Stage 59: overdue risk summary, actionable invoice list and CSV export.
  p.Children.Add(Heading(T("Overdue risk monitor", "پایش ریسک مطالبات معوق")));
  var agingStats=new WrapPanel{Margin=new Thickness(0,8,0,10)};
  p.Children.Add(agingStats);
  var agingGrid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=true,Height=260,Margin=new Thickness(0,0,0,8)};
  p.Children.Add(agingGrid);
  var agingButtons=new WrapPanel{Margin=new Thickness(0,0,0,12)};
  var agingRefresh=new Button{Content=T("Refresh overdue invoices","به‌روزرسانی معوقات"),Margin=new Thickness(0,0,8,8)};
  var agingExport=new Button{Content=T("Export overdue CSV","خروجی CSV معوقات"),Margin=new Thickness(0,0,8,8)};
  var auditView=new Button{Content=T("Due-date change history","تاریخچه تغییر سررسید"),Margin=new Thickness(0,0,8,8)};
  var dueSoon=new Button{Content=T("Due in next 7 days","سررسید ۷ روز آینده"),Margin=new Thickness(0,0,8,8)};
  agingButtons.Children.Add(agingRefresh);agingButtons.Children.Add(agingExport);agingButtons.Children.Add(auditView);agingButtons.Children.Add(dueSoon);
  p.Children.Add(agingButtons);
  List<InvoiceAllocationService.Outstanding> OverdueRows(){
    using var db=new AshkanLedgerPro.Data.LedgerDb();
    return InvoiceAllocationService.OutstandingInvoices(db,exposureDate.SelectedDate??DateTime.Today)
      .Where(x=>x.Remaining>0 && x.DueDate!=DateTime.MinValue && x.DaysPastDue>0)
      .OrderByDescending(x=>x.DaysPastDue).ThenByDescending(x=>x.Remaining).ToList();
  }
  void RefreshAging(){
    try{
      var rows=OverdueRows();agingGrid.ItemsSource=rows;agingStats.Children.Clear();
      agingStats.Children.Add(Stat(T("OVERDUE RECEIVABLES","مطالبات معوق"),rows.Where(x=>x.Kind=="Sales").Sum(x=>x.Remaining).ToString("N2"),lightTheme?"#925300":"#FFC178"));
      agingStats.Children.Add(Stat(T("OVERDUE PAYABLES","بدهی معوق"),rows.Where(x=>x.Kind=="Purchase").Sum(x=>x.Remaining).ToString("N2"),lightTheme?"#087E72":"#26D2B0"));
      agingStats.Children.Add(Stat(T("OVERDUE INVOICES","فاکتورهای معوق"),rows.Count.ToString(CultureInfo.InvariantCulture),lightTheme?"#925300":"#FFC178"));
    }catch(Exception ex){ShowOperationError(ex,"Overdue monitor","خطای پایش معوقات");}
  }
  dueSoon.Click+=(_,_)=>{
    try{
      using var db=new AshkanLedgerPro.Data.LedgerDb();
      var today=DateTime.Today;
      var upcoming=InvoiceAllocationService.OutstandingInvoices(db,today)
        .Where(x=>x.Remaining>0 && x.DueDate!=DateTime.MinValue && x.DueDate.Date>=today && x.DueDate.Date<=today.AddDays(7))
        .OrderBy(x=>x.DueDate).ThenByDescending(x=>x.Remaining).ToList();
      var window=new Window{Title=T("Upcoming invoice due dates","سررسیدهای نزدیک"),Width=1000,Height=500,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner};
      var layout=new DockPanel{Margin=new Thickness(12)};
      var header=new TextBlock{Text=T($"{upcoming.Count} invoices due within 7 days",$"{upcoming.Count} فاکتور با سررسید تا ۷ روز آینده"),Margin=new Thickness(0,0,0,12),FontSize=17};
      DockPanel.SetDock(header,Dock.Top);layout.Children.Add(header);
      layout.Children.Add(new DataGrid{ItemsSource=upcoming,IsReadOnly=true,AutoGenerateColumns=true});
      window.Content=layout;window.ShowDialog();
    }catch(Exception ex){ShowOperationError(ex,"Upcoming due dates","خطای سررسیدهای نزدیک");}
  };
  agingRefresh.Click+=(_,_)=>RefreshAging();
  agingExport.Click+=(_,_)=>{
    try{
      var dialog=new SaveFileDialog{Filter="CSV files (*.csv)|*.csv",FileName="overdue-invoices.csv"};
      if(dialog.ShowDialog()!=true)return;
      var lines=new List<string>{"Kind,InvoiceId,PartnerId,InvoiceDate,DueDate,DaysPastDue,Total,Allocated,Remaining,AgingBucket"};
      foreach(var x in OverdueRows())lines.Add(string.Join(",",new[]{x.Kind,x.InvoiceId.ToString(CultureInfo.InvariantCulture),x.PartnerId.ToString(CultureInfo.InvariantCulture),
        x.InvoiceDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),x.DueDate.ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),x.DaysPastDue.ToString(CultureInfo.InvariantCulture),
        x.Total.ToString(CultureInfo.InvariantCulture),x.Allocated.ToString(CultureInfo.InvariantCulture),x.Remaining.ToString(CultureInfo.InvariantCulture),x.AgingBucket}));
      File.WriteAllLines(dialog.FileName,lines,new UTF8Encoding(true));
      MessageBox.Show(T("Overdue report exported.","گزارش معوقات صادر شد."));
    }catch(Exception ex){ShowOperationError(ex,"Overdue export","خطای خروجی معوقات");}
  };
  auditView.Click+=(_,_)=>{
    try{
      using var db=new AshkanLedgerPro.Data.LedgerDb();
      var history=InvoiceAllocationService.DueDateHistory(db);
      var dialog=new Window{Title=T("Due-date history","تاریخچه سررسیدها"),Width=850,Height=480,Owner=this,WindowStartupLocation=WindowStartupLocation.CenterOwner};
      dialog.Content=new DataGrid{ItemsSource=history,IsReadOnly=true,AutoGenerateColumns=true,Margin=new Thickness(12)};
      dialog.ShowDialog();
    }catch(Exception ex){ShowOperationError(ex,"Due-date history","خطای تاریخچه سررسید");}
  };
  RefreshAging();

  p.Children.Add(Heading(T("Recent activity","آخرین فعالیت‌ها")));
  p.Children.Add(new DataGrid{ItemsSource=entries.Take(10).Select(x=>new{x.Id,x.Date,x.Description,Debit=x.Lines.Sum(y=>y.Debit),Credit=x.Lines.Sum(y=>y.Credit)}).ToList(),IsReadOnly=true,Height=270});
  SetWorkspace(p);
 }
 // Stage 35: read-only, cross-module search. No accounting records are modified.
 sealed record SearchResult(string Category, string Reference, string Description, string Date, string Amount);
 void GlobalSearch_Click(object sender, RoutedEventArgs e){
  currentPage=()=>GlobalSearch_Click(null!,new RoutedEventArgs());
  var panel=Panel();
  panel.Children.Add(Heading(T("Global search","جست‌وجوی سراسری")));
  panel.Children.Add(Info(T("Search accounts, journal entries, business contacts, inventory and checks. Results are read-only.","جست‌وجو در حساب‌ها، اسناد، اشخاص، کالاها و چک‌ها؛ نتایج فقط خواندنی هستند.")));
  var query=new TextBox{MinWidth=280,Margin=new Thickness(0,8,0,8),ToolTip=T("Search by number, name, description or code","جست‌وجو بر اساس شماره، نام، شرح یا کد")};
  panel.Children.Add(query);
  var category=new ComboBox{MinWidth=210,Margin=new Thickness(0,0,0,8),ItemsSource=persian?new[]{"همه","حساب‌ها","اسناد","اشخاص","کالاها","چک‌ها"}:new[]{"All","Accounts","Journal","Contacts","Inventory","Checks"},SelectedIndex=0};
  panel.Children.Add(category);
  var results=new DataGrid{IsReadOnly=true,AutoGenerateColumns=true,Height=410,Margin=new Thickness(0,8,0,8)};
  var summary=Info("");
  var export=new Button{Content=T("Export filtered CSV","خروجی CSV نتایج"),MinWidth=170,Margin=new Thickness(0,8,0,8)};
  var exportXlsx=new Button{Content=T("Export filtered XLSX","خروجی اکسل XLSX"),MinWidth=170,Margin=new Thickness(0,0,0,8)};
  panel.Children.Add(export);panel.Children.Add(exportXlsx);panel.Children.Add(summary);panel.Children.Add(results);
  List<SearchResult> all;
  try{
   all=new List<SearchResult>();
   all.AddRange(ledger.Accounts().Select(a=>new SearchResult("Accounts",a.Code,a.Name,a.Type,"")));
   all.AddRange(ledger.Entries().Select(e=>new SearchResult("Journal",e.Id.ToString(CultureInfo.InvariantCulture),e.Description,LocalizedFinancialDisplay.FormatDate(e.Date,persian),e.Lines.Sum(l=>l.Debit).ToString("N2",CultureInfo.InvariantCulture))));
   all.AddRange(business.Partners().Select(c=>new SearchResult("Contacts",c.Id.ToString(CultureInfo.InvariantCulture),c.Name+" | "+c.Kind+" | "+c.Phone+" | "+c.Email,"","")));
   all.AddRange(business.Stock().Select(i=>new SearchResult("Inventory",i.Sku,i.Name,"",i.UnitPrice.ToString("N2",CultureInfo.InvariantCulture))));
   all.AddRange(checks.Recent().Select(c=>new SearchResult("Checks",c.Number,c.Bank+" | "+c.Status+" | "+(c.Partner?.Name??""),LocalizedFinancialDisplay.FormatDate(c.DueDate,persian),c.Amount.ToString("N2",CultureInfo.InvariantCulture))));
  }catch(Exception ex){ShowOperationError(ex,"Global search","خطای جست‌وجو");return;}
  List<SearchResult> filtered=new();
  void Refresh(){
   var text=query.Text.Trim();var selected=category.SelectedIndex;
   string[] keys={"","Accounts","Journal","Contacts","Inventory","Checks"};
   filtered=all.Where(r=>(selected<=0||r.Category==keys[selected]) && (text.Length==0||
    r.Reference.Contains(text,StringComparison.OrdinalIgnoreCase)||r.Description.Contains(text,StringComparison.OrdinalIgnoreCase)||
    r.Date.Contains(text,StringComparison.OrdinalIgnoreCase)||r.Amount.Contains(text,StringComparison.OrdinalIgnoreCase))).Take(500).ToList();
   results.ItemsSource=filtered;
   summary.Text=T($"{filtered.Count} results (maximum 500)",$"{filtered.Count} نتیجه (حداکثر ۵۰۰ مورد)");
  }
  query.TextChanged+=(_,_)=>Refresh();category.SelectionChanged+=(_,_)=>Refresh();
  export.Click+=(_,_)=>{
   try{
    var dialog=new SaveFileDialog{Title=T("Export search results","خروجی نتایج جست‌وجو"),Filter="CSV files (*.csv)|*.csv",FileName="ashkan-ledger-search.csv"};
    if(dialog.ShowDialog()!=true)return;
    static string EscapeCsv(string? value){
     var text=value??"";
     // Prevent spreadsheet formula execution when opening untrusted text in Excel.
     if(text.Length>0 && (text[0]=='='||text[0]=='+'||text[0]=='-'||text[0]=='@'||text[0]=='\t'||text[0]=='\r'))text="'"+text;
     return "\""+text.Replace("\"","\"\"")+"\"";
    }
    var lines=new List<string>{"Category,Reference,Description,Date,Amount"};
    lines.AddRange(filtered.Select(r=>string.Join(",",new[]{EscapeCsv(r.Category),EscapeCsv(r.Reference),EscapeCsv(r.Description),EscapeCsv(r.Date),EscapeCsv(r.Amount)})));
    File.WriteAllLines(dialog.FileName,lines,new UTF8Encoding(true));
    MessageBox.Show(T("Search results exported.","نتایج جست‌وجو ذخیره شدند."));
   }catch(Exception ex){ShowOperationError(ex,"Search export failed","خطای خروجی جست‌وجو");}
  };
  exportXlsx.Click+=(_,_)=>{
   try{
    var dialog=new SaveFileDialog{Title=T("Export search results to Excel","خروجی اکسل نتایج جست‌وجو"),Filter="Excel workbook (*.xlsx)|*.xlsx",FileName="ashkan-ledger-search.xlsx"};
    if(dialog.ShowDialog()!=true)return;
    XlsxExportService.Save(dialog.FileName,new[]{"Category","Reference","Description","Date","Amount"},
      filtered.Select(r=>(IReadOnlyList<string?>)new string?[]{r.Category,r.Reference,r.Description,r.Date,r.Amount}));
    MessageBox.Show(T("Excel workbook exported successfully.","فایل اکسل با موفقیت ذخیره شد."));
   }catch(Exception ex){ShowOperationError(ex,"Excel export failed","خطای خروجی اکسل");}
  };
  Refresh();SetWorkspace(panel);
 }
 void Statement_Click(object sender,RoutedEventArgs e){currentPage=()=>Statement_Click(null!,new RoutedEventArgs());
  var p=Panel();p.Children.Add(Heading("Account statement"));p.Children.Add(Info("Select an account to review its chronological debit and credit movements and running balance."));
  var accounts=ledger.Accounts();var selector=new ComboBox{ItemsSource=accounts,DisplayMemberPath="Name",SelectedIndex=accounts.Count>0?0:-1,MinWidth=280};
  p.Children.Add(selector);var grid=new DataGrid{IsReadOnly=true,Height=370,Margin=new Thickness(0,14,0,14)};
  var summary=Info("");
  void Refresh(){if(selector.SelectedItem is not Account account){grid.ItemsSource=null;summary.Text="No account selected";return;}
   decimal balance=0;var rows=new List<StatementRow>();
   foreach(var entry in ledger.Entries().OrderBy(x=>x.Date).ThenBy(x=>x.Id))foreach(var line in entry.Lines.Where(x=>x.AccountId==account.Id)){
    balance+=line.Debit-line.Credit;rows.Add(new StatementRow(entry.Id,entry.Date,entry.Description,line.Debit,line.Credit,balance));}
   grid.ItemsSource=rows;summary.Text=$"Account {account.Code} • {rows.Count} movements • Closing balance: {balance:N2} (debit positive)";
  }
  selector.SelectionChanged+=(_,_)=>Refresh();Refresh();p.Children.Add(grid);p.Children.Add(summary);SetWorkspace(p);
 }
 private sealed record StatementRow(int EntryId,DateTime Date,string Description,decimal Debit,decimal Credit,decimal RunningBalance);
 void Overview_Click(object s,RoutedEventArgs e){currentPage=()=>ShowOverview();ShowOverview();}
 void Accounts_Click(object s,RoutedEventArgs e){currentPage=()=>Accounts_Click(null!,new RoutedEventArgs());var p=Panel();p.Children.Add(Heading("Chart of accounts"));var search=new TextBox{Text="",ToolTip="Search by code, name or type"};p.Children.Add(Info("Search accounts by code, name or type"));p.Children.Add(search);var grid=new DataGrid{ItemsSource=ledger.Accounts(),IsReadOnly=true,Height=320,Margin=new Thickness(0,8,0,16)};search.TextChanged+=(_,_)=>{var q=search.Text.Trim();grid.ItemsSource=ledger.Accounts().Where(a=>a.Code.Contains(q,StringComparison.OrdinalIgnoreCase)||a.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||a.Type.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();};p.Children.Add(grid);p.Children.Add(Info("Add an account"));var row=new WrapPanel();var code=new TextBox{Width=105,Text="6000"};var name=new TextBox{Width=200,Text="New account"};var type=new ComboBox{Width=140,ItemsSource=new[]{"Asset","Liability","Equity","Revenue","Expense"},SelectedIndex=0,Margin=new Thickness(4)};var add=new Button{Content="+ Add account"};add.Click+=(_,_)=>{if(!CanEdit())return;try{ledger.AddAccount(code.Text,name.Text,type.SelectedItem?.ToString()??"");Accounts_Click(s,e);}catch(Exception ex){ShowOperationError(ex, "Validation error", "خطای عملیات");}};row.Children.Add(code);row.Children.Add(name);row.Children.Add(type);row.Children.Add(add);p.Children.Add(row);SetWorkspace(p);}
 void Journal_Click(object s,RoutedEventArgs e){currentPage=()=>Journal_Click(null!,new RoutedEventArgs());var accounts=ledger.Accounts();var p=Panel();p.Children.Add(Heading("Post a journal entry"));var desc=new TextBox{Text=""};var debit=new ComboBox{ItemsSource=accounts,DisplayMemberPath="Name",SelectedIndex=0,Margin=new Thickness(4)};var credit=new ComboBox{ItemsSource=accounts,DisplayMemberPath="Name",SelectedIndex=accounts.Count>1?1:0,Margin=new Thickness(4)};var amount=new TextBox{Text="1000.00"};p.Children.Add(Info("Description"));p.Children.Add(desc);p.Children.Add(Info("Debit account"));p.Children.Add(debit);p.Children.Add(Info("Credit account"));p.Children.Add(credit);p.Children.Add(Info("Amount"));p.Children.Add(amount);var post=new Button{Content="✓ Post balanced entry",Margin=new Thickness(4,14,4,14)};post.Click+=(_,_)=>{if(!CanEdit())return;try{if(debit.SelectedItem is not Account d||credit.SelectedItem is not Account c)throw new ArgumentException("Select both accounts.");if(!decimal.TryParse(amount.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var value)&&!decimal.TryParse(amount.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out value))throw new ArgumentException("Enter a valid amount.");ledger.Post(desc.Text,d.Id,c.Id,value);Journal_Click(s,e);}catch(Exception ex){ShowOperationError(ex, "Validation error", "خطای عملیات");}};p.Children.Add(post);p.Children.Add(Heading("Recent entries"));p.Children.Add(new DataGrid{ItemsSource=ledger.Entries().Select(x=>new{x.Id,x.Date,x.Description,x.Posted,Debit=x.Lines.Sum(y=>y.Debit),Credit=x.Lines.Sum(y=>y.Credit)}).ToList(),IsReadOnly=true,Height=210});SetWorkspace(p);}
 void ExportFinancialXlsx(string reportName,IReadOnlyList<string> headers,IEnumerable<IReadOnlyList<string?>> records)
 {
  var dialog=new SaveFileDialog{Filter="Excel workbook (*.xlsx)|*.xlsx",DefaultExt=".xlsx",AddExtension=true,
   FileName=reportName.Replace(' ', '_')+".xlsx"};
  if(dialog.ShowDialog()!=true)return;
  try{XlsxExportService.Save(dialog.FileName,headers,records);
   MessageBox.Show(T("Excel report saved successfully.","گزارش اکسل با موفقیت ذخیره شد."),T("Export","خروجی"));}
  catch(Exception ex){ShowOperationError(ex,"Excel export error","خطای خروجی اکسل");}
 }
 void AccountLedgerReport_Click(object sender, RoutedEventArgs e)
 {
  currentPage=()=>AccountLedgerReport_Click(null!,new RoutedEventArgs());
  var panel=Panel();
  panel.Children.Add(Heading(T("General ledger details","ریز گردش دفتر کل")));
  panel.Children.Add(Info(T("Select an account and period to see opening balance, journal movements and running balance.","حساب و بازه زمانی را برای مشاهده مانده افتتاحیه، گردش اسناد و مانده جاری انتخاب کنید.")));
  var accounts=ledger.Accounts();
  var picker=new ComboBox{Width=310,Margin=new Thickness(5),DisplayMemberPath="Name",SelectedValuePath="Id",ItemsSource=accounts};
  if(accounts.Count>0)picker.SelectedIndex=0;
  var fromBox=new TextBox{Width=150,Margin=new Thickness(5),Text=LocalizedDateInput.Format(DateTime.Today.AddMonths(-1),persian)};
  var toBox=new TextBox{Width=150,Margin=new Thickness(5),Text=LocalizedDateInput.Format(DateTime.Today,persian)};
  var filters=new WrapPanel{Margin=new Thickness(0,8,0,8)};
  filters.Children.Add(new TextBlock{Text=T("Account:","حساب:"),VerticalAlignment=VerticalAlignment.Center});filters.Children.Add(picker);
  filters.Children.Add(new TextBlock{Text=T("From:","از:"),VerticalAlignment=VerticalAlignment.Center});filters.Children.Add(fromBox);
  filters.Children.Add(new TextBlock{Text=T("To:","تا:"),VerticalAlignment=VerticalAlignment.Center});filters.Children.Add(toBox);
  panel.Children.Add(filters);
  var actions=new WrapPanel();
  var generate=new Button{Content=T("Generate ledger","نمایش گردش حساب"),Margin=new Thickness(5)};
  var export=new Button{Content=T("Export XLSX","خروجی اکسل"),Margin=new Thickness(5)};
  actions.Children.Add(generate);actions.Children.Add(export);panel.Children.Add(actions);
  var summary=new TextBlock{TextWrapping=TextWrapping.Wrap,Margin=new Thickness(5,10,5,10)};panel.Children.Add(summary);
  var table=new DataGrid{IsReadOnly=true,AutoGenerateColumns=true,Height=410,Margin=new Thickness(5)};
  panel.Children.Add(table);
  AccountLedgerReportService.LedgerResult? latest=null;
  void Generate()
  {
   if(picker.SelectedValue is not int id || !LocalizedDateInput.TryParse(fromBox.Text,persian,out var from) ||
      !LocalizedDateInput.TryParse(toBox.Text,persian,out var to) || from.Date>to.Date)
   {MessageBox.Show(T("Select an account and valid dates.","یک حساب و بازه تاریخ معتبر انتخاب کنید."));return;}
   try
   {
    latest=new AccountLedgerReportService().GetAccountLedger(id,from,to);
    table.ItemsSource=latest.Rows.Select(r=>new {
     r.EntryId,Date=LocalizedDateInput.Format(r.Date,persian),r.Description,r.Debit,r.Credit,r.RunningBalance
    }).ToList();
    summary.Text=T($"Opening: {latest.OpeningBalance:N2} | Debit: {latest.PeriodDebit:N2} | Credit: {latest.PeriodCredit:N2} | Closing: {latest.ClosingBalance:N2}",
     $"افتتاحیه: {latest.OpeningBalance:N2} | بدهکار: {latest.PeriodDebit:N2} | بستانکار: {latest.PeriodCredit:N2} | اختتامیه: {latest.ClosingBalance:N2}");
   }
   catch(Exception ex){ShowOperationError(ex,"General ledger error","خطای گردش دفتر کل");}
  }
  generate.Click+=(_,_)=>Generate();
  export.Click+=(_,_)=>
  {
   if(latest is null){MessageBox.Show(T("Generate the ledger first.","ابتدا گزارش را نمایش دهید."));return;}
   var dialog=new SaveFileDialog{Filter="Excel workbook (*.xlsx)|*.xlsx",DefaultExt=".xlsx",AddExtension=true,FileName="Account_Ledger.xlsx"};
   if(dialog.ShowDialog()!=true)return;
   try
   {
    XlsxExportService.SaveTyped(dialog.FileName,
     new[]{T("Entry","سند"),T("Date","تاریخ"),T("Description","شرح"),T("Debit","بدهکار"),T("Credit","بستانکار"),T("Running balance","مانده جاری")},
     latest.Rows.Select(r=>(IReadOnlyList<object?>)new object?[]{r.EntryId.ToString(),LocalizedDateInput.Format(r.Date,persian),r.Description,r.Debit,r.Credit,r.RunningBalance}));
    MessageBox.Show(T("Excel report saved.","گزارش اکسل ذخیره شد."));
   }
   catch(Exception ex){ShowOperationError(ex,"Excel export error","خطای خروجی اکسل");}
  };
  SetWorkspace(panel);
 }
 void FiscalPeriods_Click(object sender,RoutedEventArgs e)
 {
  currentPage=()=>FiscalPeriods_Click(null!,new RoutedEventArgs());
  var service=new FiscalPeriodService();
  var p=Panel();p.Children.Add(Heading(T("Fiscal period management","مدیریت دوره‌های مالی")));
  p.Children.Add(Info(T("Only administrators can create or lock periods. Locking blocks new journal postings for dates in that period.","فقط مدیر می‌تواند دوره ایجاد یا قفل کند. قفل دوره مانع ثبت سند جدید در آن بازه می‌شود.")));
  var periods=service.List();
  var table=new DataGrid{ItemsSource=periods.Select(x=>new {x.Id,x.Name,Start=LocalizedDateInput.Format(x.Start,persian),End=LocalizedDateInput.Format(x.End,persian),x.Locked}).ToList(),IsReadOnly=true,AutoGenerateColumns=true,Height=250,Margin=new Thickness(4)};
  p.Children.Add(table);
  if(currentUser.Role=="Admin")
  {
   var name=new TextBox{Width=190,Margin=new Thickness(4),Text=T("Fiscal year","سال مالی")};
   var from=new TextBox{Width=140,Margin=new Thickness(4),Text=LocalizedDateInput.Format(new DateTime(DateTime.Today.Year,1,1),persian)};
   var to=new TextBox{Width=140,Margin=new Thickness(4),Text=LocalizedDateInput.Format(new DateTime(DateTime.Today.Year,12,31),persian)};
   var row=new WrapPanel();row.Children.Add(name);row.Children.Add(from);row.Children.Add(to);
   var add=new Button{Content=T("Create period","ایجاد دوره"),Margin=new Thickness(4)};
   add.Click+=(_,_)=>{try{if(!LocalizedDateInput.TryParse(from.Text,persian,out var a)||!LocalizedDateInput.TryParse(to.Text,persian,out var b))throw new ArgumentException("Invalid dates.");service.Create(name.Text,a,b,currentUser.Username);FiscalPeriods_Click(sender,e);}catch(Exception ex){ShowOperationError(ex,"Fiscal period error","خطای دوره مالی");}};
   row.Children.Add(add);p.Children.Add(row);
   var id=new TextBox{Width=85,Margin=new Thickness(4)};
   var lockRow=new WrapPanel();lockRow.Children.Add(new TextBlock{Text=T("Period ID:","شناسه دوره:"),VerticalAlignment=VerticalAlignment.Center});lockRow.Children.Add(id);
   foreach(var state in new[]{true,false})
   {
    var btn=new Button{Content=state?T("Lock period","قفل دوره"):T("Unlock period","بازکردن قفل"),Margin=new Thickness(4)};
    btn.Click+=(_,_)=>{try{if(!int.TryParse(id.Text,out var periodId))throw new ArgumentException("Invalid period ID.");if(MessageBox.Show(T("Confirm fiscal period change?","تغییر وضعیت دوره مالی تأیید شود؟"),T("Confirmation","تأیید"),MessageBoxButton.YesNo)!=MessageBoxResult.Yes)return;service.SetLocked(periodId,state,currentUser.Username);FiscalPeriods_Click(sender,e);}catch(Exception ex){ShowOperationError(ex,"Fiscal period error","خطای دوره مالی");}};
    lockRow.Children.Add(btn);
   }
   p.Children.Add(lockRow);
  }
  var inspectButton=new Button{Content=T("Check database protection", "بررسی محافظت دیتابیس"),Margin=new Thickness(4)};
  inspectButton.Click+=(_,_)=>
  {
   try
   {
    var checks=FiscalGuardDiagnostics.Inspect();
    var missing=checks.Where(x=>!x.Complete).ToList();
    var details=string.Join(Environment.NewLine,checks.Select(x=>$"{x.Table}: {x.TriggerCount}/3"));
    MessageBox.Show((missing.Count==0?T("All expected fiscal triggers are installed.","تمام تریگرهای مورد انتظار نصب شده‌اند."):T("Some fiscal triggers are missing!", "برخی تریگرهای محافظت مالی نصب نشده‌اند!"))+Environment.NewLine+details,T("Fiscal protection check", "بررسی محافظت دوره مالی"),MessageBoxButton.OK,missing.Count==0?MessageBoxImage.Information:MessageBoxImage.Warning);
   }
   catch(Exception ex){ShowOperationError(ex,"Fiscal protection check failed","خطا در بررسی محافظت دوره مالی");}
  };
  p.Children.Add(inspectButton);
  if (currentUser.Role == "Admin")
  {
   var securityEventsButton = new Button { Content = T("Security events", "رویدادهای امنیتی"), Margin = new Thickness(4) };
   securityEventsButton.Click += (_, _) =>
   {
    try
    {
     var security = new SecurityEventService();
     var content = new DockPanel { Margin = new Thickness(12) };
     var filters = new WrapPanel { Margin = new Thickness(0,0,0,10) };
     var actorFilter = new TextBox { Width=150, ToolTip=T("Actor contains", "جست‌وجوی کاربر"), Margin=new Thickness(4) };
     var typeFilter = new ComboBox { Width=185, Margin=new Thickness(4), IsEditable=false,
       ItemsSource=new[]{"All", "FiscalWriteDenied"}, SelectedIndex=0 };
     var refresh = new Button { Content=T("Filter", "فیلتر"), Margin=new Thickness(4) };
     var export = new Button { Content=T("Export CSV", "خروجی CSV"), Margin=new Thickness(4) };
     var summary = new TextBlock { Margin=new Thickness(4,8,4,8) };
     filters.Children.Add(actorFilter);filters.Children.Add(typeFilter);filters.Children.Add(refresh);filters.Children.Add(export);
     var header = new StackPanel();header.Children.Add(filters);header.Children.Add(summary);
     DockPanel.SetDock(header,Dock.Top);content.Children.Add(header);
     var grid = new DataGrid { IsReadOnly=true, AutoGenerateColumns=true, Margin=new Thickness(0,4,0,0) };
     content.Children.Add(grid);
     List<SecurityEventService.SecurityEvent> shown = new();
     void Reload()
     {
      shown=security.Search(actorFilter.Text, typeFilter.SelectedItem?.ToString()=="All"?null:typeFilter.SelectedItem?.ToString());
      grid.ItemsSource=shown.Select(x=>new {x.Id,OccurredUtc=x.OccurredUtc.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture),x.Actor,x.EventType,x.Context,x.Message}).ToList();
      summary.Text=T($"Showing {shown.Count} events (maximum 500).",$"تعداد رویدادهای نمایش‌داده‌شده: {shown.Count} (حداکثر ۵۰۰)");
     }
     refresh.Click+=(_,_)=>{try{Reload();}catch(Exception ex){MessageBox.Show(ex.Message);}};
     export.Click+=(_,_)=>
     {
      try
      {
       var file=new SaveFileDialog { Filter="CSV files (*.csv)|*.csv",FileName="ashkan-security-events.csv" };
       if(file.ShowDialog()!=true)return;
       var csv=new StringBuilder("Id,OccurredUtc,Actor,EventType,Context,Message\r\n");
       foreach(var item in shown)
       {
        string Quote(string? value)=>"\""+(value??"").Replace("\"","\"\"")+"\"";
        csv.Append(item.Id.ToString(CultureInfo.InvariantCulture)).Append(',')
           .Append(Quote(item.OccurredUtc.ToString("O",CultureInfo.InvariantCulture))).Append(',')
           .Append(Quote(item.Actor)).Append(',').Append(Quote(item.EventType)).Append(',')
           .Append(Quote(item.Context)).Append(',').Append(Quote(item.Message)).Append("\r\n");
       }
       File.WriteAllText(file.FileName,csv.ToString(),new UTF8Encoding(true));
       MessageBox.Show(T("Security event CSV exported.","خروجی رویدادهای امنیتی ذخیره شد."));
      }
      catch(Exception ex){MessageBox.Show(ex.Message);}
     };
     var dialog = new Window { Title=T("Security event center","مرکز رویدادهای امنیتی"),
       Width=1100,Height=640,MinWidth=760,MinHeight=440,Owner=this,
       WindowStartupLocation=WindowStartupLocation.CenterOwner,
       FlowDirection=persian?FlowDirection.RightToLeft:FlowDirection.LeftToRight,
       Content=content };
     Reload();dialog.ShowDialog();
    }
    catch(Exception ex){ShowOperationError(ex,"Security events","خطای رویدادهای امنیتی");}
   };
   p.Children.Add(securityEventsButton);
  }
  p.Children.Add(Heading(T("Fiscal period audit history", "تاریخچه حسابرسی دوره‌های مالی")));
  p.Children.Add(Info(T("Latest 500 period events. Records are read-only and timestamps are UTC.",
      "۵۰۰ رویداد آخر دوره‌ها؛ اطلاعات فقط خواندنی است و زمان‌ها بر اساس UTC ثبت شده‌اند.")));
  var auditRows=service.AuditTrail().Select(a=>new {
      a.Id, a.PeriodId,
      Action=a.Action=="Create"?T("Created","ایجاد"):a.Action=="Lock"?T("Locked","قفل شد"):T("Unlocked","قفل باز شد"),
      a.Actor, OccurredUtc=a.OccurredUtc.ToString("yyyy-MM-dd HH:mm:ss",CultureInfo.InvariantCulture)
  }).ToList();
  p.Children.Add(new DataGrid{ItemsSource=auditRows,IsReadOnly=true,AutoGenerateColumns=true,Height=260,Margin=new Thickness(4)});
  SetWorkspace(p);
 }
 void PeriodReport_Click(object s,RoutedEventArgs e)
 {
  currentPage=()=>PeriodReport_Click(null!,new RoutedEventArgs());
  var p=Panel();p.Children.Add(Heading(T("Financial period reports","گزارش‌های دوره‌ای مالی")));
  p.Children.Add(Info(T("Choose a date range. Persian mode uses the Jalali calendar; English mode uses Gregorian dates.","بازه تاریخ را انتخاب کنید. در حالت فارسی تاریخ‌ها شمسی و در حالت انگلیسی میلادی هستند.")));
  var dates=new WrapPanel{Margin=new Thickness(0,8,0,12)};
  var start=new TextBox{Width=150,Margin=new Thickness(5),Text=LocalizedDateInput.Format(DateTime.Today.AddMonths(-1),persian)};
  var end=new TextBox{Width=150,Margin=new Thickness(5),Text=LocalizedDateInput.Format(DateTime.Today,persian)};
  dates.Children.Add(new TextBlock{Text=T("From:","از تاریخ:"),VerticalAlignment=VerticalAlignment.Center});dates.Children.Add(start);
  dates.Children.Add(new TextBlock{Text=T("To:","تا تاریخ:"),VerticalAlignment=VerticalAlignment.Center});dates.Children.Add(end);
  p.Children.Add(dates);
  // Fiscal-year presets only select a reporting range; they do not close or modify the ledger.
  var presets=new WrapPanel{Margin=new Thickness(0,2,0,10)};
  var thisYear=new Button{Content=T("Current fiscal year","سال مالی جاری"),Margin=new Thickness(4)};
  var previousYear=new Button{Content=T("Previous fiscal year","سال مالی گذشته"),Margin=new Thickness(4)};
  void SetYearRange(int offset)
  {
   DateTime first, last;
   if(persian)
   {
    var calendar=new PersianCalendar();
    var year=calendar.GetYear(DateTime.Today)+offset;
    first=calendar.ToDateTime(year,1,1,0,0,0,0);
    last=calendar.ToDateTime(year+1,1,1,0,0,0,0).AddDays(-1);
   }
   else
   {
    var year=DateTime.Today.Year+offset;
    first=new DateTime(year,1,1);
    last=new DateTime(year,12,31);
   }
   start.Text=LocalizedDateInput.Format(first,persian);
   end.Text=LocalizedDateInput.Format(last,persian);
  }
  thisYear.Click+=(_,_)=>{try{SetYearRange(0);}catch(Exception ex){ShowOperationError(ex,"Fiscal year error","خطای سال مالی");}};
  previousYear.Click+=(_,_)=>{try{SetYearRange(-1);}catch(Exception ex){ShowOperationError(ex,"Fiscal year error","خطای سال مالی");}};
  presets.Children.Add(thisYear);presets.Children.Add(previousYear);p.Children.Add(presets);var managePeriods=new Button{Content=T("Manage fiscal periods","مدیریت دوره‌های مالی"),Margin=new Thickness(4)};managePeriods.Click+=FiscalPeriods_Click;p.Children.Add(managePeriods);
  p.Children.Add(Info(T("Fiscal year shortcuts use January–December in English and Farvardin–Esfand in Persian. No accounting entries are changed.","میان‌برهای سال مالی در انگلیسی از ژانویه تا دسامبر و در فارسی از فروردین تا اسفند هستند. هیچ سندی تغییر نمی‌کند.")));
  var actions=new WrapPanel();var refresh=new Button{Content=T("Generate report","نمایش گزارش"),Margin=new Thickness(4)};
  var export=new Button{Content=T("Export XLSX","خروجی اکسل"),Margin=new Thickness(4)};
  actions.Children.Add(refresh);actions.Children.Add(export);var details=new Button{Content=T("General ledger details","ریز گردش دفتر کل"),Margin=new Thickness(4)};details.Click+=AccountLedgerReport_Click;actions.Children.Add(details);p.Children.Add(actions);
  var summary=new TextBlock{Margin=new Thickness(4,12,4,8),TextWrapping=TextWrapping.Wrap};p.Children.Add(summary);
  var grid=new DataGrid{IsReadOnly=true,AutoGenerateColumns=true,Height=380,Margin=new Thickness(0,8,0,8)};p.Children.Add(grid);
  IReadOnlyList<FinancialPeriodReportService.BalanceRow>? last=null;
  void Refresh()
  {
   if(!LocalizedDateInput.TryParse(start.Text,persian,out var from)||!LocalizedDateInput.TryParse(end.Text,persian,out var to)||from.Date>to.Date)
   {MessageBox.Show(T("Enter a valid date range.","بازه تاریخ معتبر وارد کنید."));return;}
   try
   {
    last=new FinancialPeriodReportService().GetBalances(from,to);
    var rows=last.Select(x=>new { x.Code,x.Name,x.Type,
      x.OpeningDebit,x.OpeningCredit,x.Debit,x.Credit,x.ClosingDebit,x.ClosingCredit }).ToList();
    grid.ItemsSource=rows;
    summary.Text=T($"Opening balance: {last.Sum(x=>x.Opening):N2} | Period debit: {last.Sum(x=>x.Debit):N2} | Period credit: {last.Sum(x=>x.Credit):N2} | Closing balance: {last.Sum(x=>x.Closing):N2}",
      $"مانده افتتاحیه: {last.Sum(x=>x.Opening):N2} | بدهکار دوره: {last.Sum(x=>x.Debit):N2} | بستانکار دوره: {last.Sum(x=>x.Credit):N2} | مانده اختتامیه: {last.Sum(x=>x.Closing):N2}");
   }
   catch(Exception ex){ShowOperationError(ex,"Period report error","خطای گزارش دوره‌ای");}
  }
  var audit=new Button{Content=T("Validate journal balance","کنترل توازن اسناد"),Margin=new Thickness(4)};
  actions.Children.Add(audit);
  audit.Click+=(_,_)=>
  {
   try
   {
    if(!LocalizedDateInput.TryParse(start.Text,persian,out var from)||!LocalizedDateInput.TryParse(end.Text,persian,out var to)||from.Date>to.Date)
    {MessageBox.Show(T("Enter a valid date range.","بازه تاریخ معتبر وارد کنید."));return;}
    var result=new FinancialPeriodReportService().ValidatePostedEntries(from,to);
    var message=T($"Posted entries: {result.EntryCount} | Unbalanced: {result.UnbalancedCount} | Debit: {result.TotalDebit:N2} | Credit: {result.TotalCredit:N2}",
      $"اسناد ثبت‌شده: {result.EntryCount} | نامتوازن: {result.UnbalancedCount} | بدهکار: {result.TotalDebit:N2} | بستانکار: {result.TotalCredit:N2}");
    MessageBox.Show(message,T("Journal validation","کنترل اسناد"));
   }
   catch(Exception ex){ShowOperationError(ex,"Journal validation error","خطای کنترل اسناد");}
  };
  refresh.Click+=(_,_)=>Refresh();
  export.Click+=(_,_)=>
  {
   if(last is null){MessageBox.Show(T("Generate the report first.","ابتدا گزارش را نمایش دهید."));return;}
   var dialog=new SaveFileDialog{Filter="Excel workbook (*.xlsx)|*.xlsx",DefaultExt=".xlsx",AddExtension=true,FileName="Financial_Period_Report.xlsx"};
   if(dialog.ShowDialog()!=true)return;
   try
   {
    XlsxExportService.SaveTyped(dialog.FileName,
     new[]{T("Code","کد"),T("Account","حساب"),T("Type","نوع"),T("Opening debit","بدهکار افتتاحیه"),T("Opening credit","بستانکار افتتاحیه"),T("Period debit","بدهکار دوره"),T("Period credit","بستانکار دوره"),T("Closing debit","بدهکار اختتامیه"),T("Closing credit","بستانکار اختتامیه")},
     last.Select(x=>(IReadOnlyList<object?>)new object?[]{x.Code,x.Name,x.Type,x.OpeningDebit,x.OpeningCredit,x.Debit,x.Credit,x.ClosingDebit,x.ClosingCredit}));
    MessageBox.Show(T("Excel report saved successfully.","گزارش اکسل با موفقیت ذخیره شد."));
   }
   catch(Exception ex){ShowOperationError(ex,"Excel export error","خطای خروجی اکسل");}
  };
  Refresh();SetWorkspace(p);
 }
 void Trial_Click(object s,RoutedEventArgs e){currentPage=()=>Trial_Click(null!,new RoutedEventArgs());var p=Panel();p.Children.Add(Heading("Trial balance"));var balances=ledger.Balances();var rows=ledger.Accounts().Select(a=>new{a.Code,a.Name,Debit=Math.Max(0,balances.GetValueOrDefault(a.Id)),Credit=Math.Max(0,-balances.GetValueOrDefault(a.Id))}).ToList();p.Children.Add(new DataGrid{ItemsSource=rows,IsReadOnly=true,Height=390});p.Children.Add(Info(T($"Total debits: {rows.Sum(x=>x.Debit):N2}    |    Total credits: {rows.Sum(x=>x.Credit):N2}",$"جمع بدهکار: {rows.Sum(x=>x.Debit):N2}    |    جمع بستانکار: {rows.Sum(x=>x.Credit):N2}")));
  var export=new Button{Content=T("Export trial balance XLSX","خروجی اکسل تراز آزمایشی"),Margin=new Thickness(0,12,0,12),MinWidth=210};
  export.Click+=(_,_)=>ExportFinancialXlsx(T("Trial balance","تراز آزمایشی"),
   new[]{T("Account code","کد حساب"),T("Account name","نام حساب"),T("Debit","بدهکار"),T("Credit","بستانکار")},
   rows.Select(r=>(IReadOnlyList<string?>)new string?[]{r.Code,r.Name,r.Debit.ToString("0.00",CultureInfo.InvariantCulture),r.Credit.ToString("0.00",CultureInfo.InvariantCulture)}));
  p.Children.Add(export);var period=new Button{Content=T("Date range reports","گزارش‌های بازه تاریخ"),Margin=new Thickness(0,4,0,10)};period.Click+=PeriodReport_Click;p.Children.Add(period);SetWorkspace(p);}
 void Profit_Click(object s,RoutedEventArgs e){currentPage=()=>Profit_Click(null!,new RoutedEventArgs());var p=Panel();p.Children.Add(Heading("Profit & loss"));var balances=ledger.Balances();var rows=ledger.Accounts().Where(a=>a.Type is "Revenue" or "Expense").Select(a=>new{a.Code,a.Name,a.Type,Amount=a.Type=="Revenue"?-balances.GetValueOrDefault(a.Id):balances.GetValueOrDefault(a.Id)}).ToList();var revenue=rows.Where(x=>x.Type=="Revenue").Sum(x=>x.Amount);var expenses=rows.Where(x=>x.Type=="Expense").Sum(x=>x.Amount);var cards=new WrapPanel();cards.Children.Add(Stat(T("REVENUE","درآمد"),revenue.ToString("N2"),lightTheme?"#087E72":"#26D2B0"));cards.Children.Add(Stat(T("EXPENSES","هزینه‌ها"),expenses.ToString("N2"),lightTheme?"#925300":"#FFC178"));cards.Children.Add(Stat(T("NET PROFIT","سود خالص"),(revenue-expenses).ToString("N2"),lightTheme?"#135C93":"#A8D8FF"));p.Children.Add(cards);p.Children.Add(new DataGrid{ItemsSource=rows,IsReadOnly=true,Height=320});
  var export=new Button{Content=T("Export profit & loss XLSX","خروجی اکسل سود و زیان"),Margin=new Thickness(0,12,0,12),MinWidth=210};
  export.Click+=(_,_)=>ExportFinancialXlsx(T("Profit and loss","سود و زیان"),
   new[]{T("Account code","کد حساب"),T("Account name","نام حساب"),T("Type","نوع"),T("Amount","مبلغ")},
   rows.Select(r=>(IReadOnlyList<string?>)new string?[]{r.Code,r.Name,r.Type,r.Amount.ToString("0.00",CultureInfo.InvariantCulture)}));
  p.Children.Add(export);var period=new Button{Content=T("Date range reports","گزارش‌های بازه تاریخ"),Margin=new Thickness(0,4,0,10)};period.Click+=PeriodReport_Click;p.Children.Add(period);SetWorkspace(p);}
 void Partners_Click(object s,RoutedEventArgs e){currentPage=()=>Partners_Click(null!,new RoutedEventArgs());var p=Panel();p.Children.Add(Heading("Customers & suppliers"));p.Children.Add(Info("Manage your business contacts in one place."));var search=new TextBox{ToolTip="Search contacts"};p.Children.Add(search);var grid=new DataGrid{IsReadOnly=true,Height=280,Margin=new Thickness(0,12,0,18)};void Refresh(){var q=search.Text.Trim();grid.ItemsSource=business.Partners().Where(x=>x.Name.Contains(q,StringComparison.OrdinalIgnoreCase)||x.Kind.Contains(q,StringComparison.OrdinalIgnoreCase)).ToList();}search.TextChanged+=(_,_)=>Refresh();Refresh();p.Children.Add(grid);p.Children.Add(Heading("Add contact"));var name=new TextBox{ToolTip="Business name"};var kind=new ComboBox{ItemsSource=new[]{"Customer","Supplier"},SelectedIndex=0};var phone=new TextBox{ToolTip="Phone"};var email=new TextBox{ToolTip="Email"};p.Children.Add(Info("Name"));p.Children.Add(name);p.Children.Add(Info("Type"));p.Children.Add(kind);p.Children.Add(Info("Phone"));p.Children.Add(phone);p.Children.Add(Info("Email"));p.Children.Add(email);var add=new Button{Content="+ Save contact",Margin=new Thickness(0,16,0,0)};add.Click+=(_,_)=>{if(!CanEdit())return;try{business.AddPartner(name.Text,kind.SelectedItem?.ToString()??"",phone.Text,email.Text);Partners_Click(s,e);}catch(Exception ex){ShowOperationError(ex, "Validation error", "خطای عملیات");}};p.Children.Add(add);SetWorkspace(p);}
 void Inventory_Click(object s,RoutedEventArgs e){currentPage=()=>Inventory_Click(null!,new RoutedEventArgs());var p=Panel();p.Children.Add(Heading("Inventory catalog"));p.Children.Add(Info("Track product quantities and reference prices. This is a catalog, not yet a stock movement ledger."));var grid=new DataGrid{ItemsSource=business.Stock(),IsReadOnly=true,Height=270,Margin=new Thickness(0,12,0,12)};p.Children.Add(grid);p.Children.Add(Heading("Add inventory item"));var sku=new TextBox{ToolTip="SKU"};var name=new TextBox{ToolTip="Product name"};var quantity=new TextBox{Text="0"};var price=new TextBox{Text="0.00"};p.Children.Add(Info("SKU"));p.Children.Add(sku);p.Children.Add(Info("Name"));p.Children.Add(name);p.Children.Add(Info("Opening quantity"));p.Children.Add(quantity);p.Children.Add(Info("Unit price"));p.Children.Add(price);var add=new Button{Content="+ Save item",Margin=new Thickness(0,16,0,0)};add.Click+=(_,_)=>{if(!CanEdit())return;try{if(!int.TryParse(quantity.Text,out var q)||!decimal.TryParse(price.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var v))throw new ArgumentException("Enter valid quantity and price.");business.AddStock(sku.Text,name.Text,q,v);Inventory_Click(s,e);}catch(Exception ex){ShowOperationError(ex, "Validation error", "خطای عملیات");}};p.Children.Add(add);SetWorkspace(p);}
 void Movements_Click(object sender,RoutedEventArgs e){currentPage=()=>Movements_Click(null!,new RoutedEventArgs());
  var p=Panel();p.Children.Add(Heading("Stock movements"));p.Children.Add(Info("Receive or issue inventory. Stock levels update atomically, and negative inventory is blocked."));
  var items=business.Stock();var product=new ComboBox{ItemsSource=items,DisplayMemberPath="Name",SelectedIndex=items.Count>0?0:-1};
  var type=new ComboBox{ItemsSource=new[]{"IN","OUT"},SelectedIndex=0};var qty=new TextBox{Text="1"};var note=new TextBox();
  p.Children.Add(Info("Product"));p.Children.Add(product);p.Children.Add(Info("Movement type"));p.Children.Add(type);p.Children.Add(Info("Quantity"));p.Children.Add(qty);p.Children.Add(Info("Reference / reason"));p.Children.Add(note);
  var save=new Button{Content="Save stock movement",Margin=new Thickness(4,14,4,18)};
  save.Click+=(_,_)=>{if(!CanEdit())return;try{if(product.SelectedItem is not StockItem item)throw new ArgumentException("Add a product first.");if(!int.TryParse(qty.Text,out int count))throw new ArgumentException("Enter a whole-number quantity.");business.MoveStock(item.Id,type.SelectedItem?.ToString()??"",count,note.Text);Movements_Click(sender,e);}catch(Exception ex){ShowOperationError(ex, "Stock movement", "خطای عملیات");}};
  p.Children.Add(save);p.Children.Add(Heading("Latest movements"));
  p.Children.Add(new DataGrid{ItemsSource=business.Movements().Select(x=>new{x.Id,Product=x.StockItem?.Name,x.Type,x.Quantity,x.Note,DateUtc=x.OccurredAt}).ToList(),IsReadOnly=true,Height=300});SetWorkspace(p);
 }
 void Invoices_Click(object sender,RoutedEventArgs e){currentPage=()=>Invoices_Click(null!,new RoutedEventArgs());
  var p=Panel();p.Children.Add(Heading(T("Multi-item sales invoices","فاکتور فروش چندکالایی")));
  p.Children.Add(Info(T("Add products to the draft, then post all stock deductions atomically. Journal posting is not yet automated.","کالاها را به پیش‌نویس اضافه کنید؛ کسر موجودی همه اقلام در یک تراکنش انجام می‌شود. سند حسابداری هنوز خودکار نیست.")));
  var customers=business.Partners().Where(x=>x.Kind=="Customer").ToList();var stock=business.Stock();
  var partner=new ComboBox{ItemsSource=customers,DisplayMemberPath="Name",SelectedIndex=customers.Count>0?0:-1};
  var item=new ComboBox{ItemsSource=stock,DisplayMemberPath="Name",SelectedIndex=stock.Count>0?0:-1};
  var qty=new TextBox{Text="1"};var discount=new TextBox{Text="0"};var tax=new TextBox{Text="0"};
  var draft=new System.Collections.ObjectModel.ObservableCollection<InvoiceDraftLine>();
  var grid=new DataGrid{ItemsSource=draft,AutoGenerateColumns=true,IsReadOnly=true,Height=170,Margin=new Thickness(4,8,4,8)};
  p.Children.Add(Info(T("Customer","مشتری")));p.Children.Add(partner);
  p.Children.Add(Info(T("Product","کالا")));p.Children.Add(item);
  p.Children.Add(Info(T("Quantity","تعداد")));p.Children.Add(qty);
  var add=new Button{Content=T("Add item","افزودن کالا"),Margin=new Thickness(4,8,4,8)};
  add.Click+=(_,_)=>{try{if(!CanEdit())return;if(item.SelectedItem is not StockItem product)throw new ArgumentException("Select a product.");if(!int.TryParse(qty.Text,out var count)||count<=0)throw new ArgumentException("Quantity must be positive.");var existing=draft.FirstOrDefault(x=>x.StockItemId==product.Id);var total=checked(count+(existing?.Quantity??0));if(total>product.Quantity)throw new InvalidOperationException("Insufficient stock.");if(existing!=null)draft.Remove(existing);draft.Add(new InvoiceDraftLine(product.Id,product.Name,total,product.UnitPrice));}catch(Exception ex){ShowOperationError(ex, "Invoice draft", "خطای عملیات");}};
  p.Children.Add(add);p.Children.Add(grid);
  var remove=new Button{Content=T("Remove selected item","حذف کالای انتخابی")};remove.Click+=(_,_)=>{if(grid.SelectedItem is InvoiceDraftLine line)draft.Remove(line);};p.Children.Add(remove);
  p.Children.Add(Info(T("Discount %","تخفیف ٪")));p.Children.Add(discount);p.Children.Add(Info(T("Tax %","مالیات ٪")));p.Children.Add(tax);
  var save=new Button{Content=T("Post sales invoice","ثبت فاکتور فروش"),Margin=new Thickness(4,16,4,16)};
  save.Click+=(_,_)=>{if(!CanEdit())return;try{
   if(partner.SelectedItem is not BusinessPartner customer)throw new ArgumentException("Select a customer.");
   if(!decimal.TryParse(discount.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var d)||!decimal.TryParse(tax.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var t))throw new ArgumentException("Enter valid numeric values.");
   var id=invoices.PostSaleItems(customer.Id,draft.Select(x=>new InvoiceService.SaleItem(x.StockItemId,x.Quantity)).ToList(),d,t);
   MessageBox.Show(T($"Invoice #{id} saved.",$"فاکتور شماره {id} ذخیره شد."));Invoices_Click(sender,e);
  }catch(Exception ex){ShowOperationError(ex, "Invoice validation", "خطای عملیات");}};
  p.Children.Add(save);p.Children.Add(Heading(T("Recent invoices","فاکتورهای اخیر")));
  p.Children.Add(new DataGrid{ItemsSource=invoices.Recent().Select(x=>new{x.Id,Customer=x.Partner?.Name,Lines=x.Items.Count,x.CreatedAtUtc,x.Subtotal,x.Discount,x.Tax,x.Total,x.Status}).ToList(),IsReadOnly=true,Height=260});
  SetWorkspace(p);
 }
 private sealed record InvoiceDraftLine(int StockItemId,string Product,int Quantity,decimal UnitPrice){public decimal LineTotal=>Quantity*UnitPrice;}
 void Purchases_Click(object sender,RoutedEventArgs e){currentPage=()=>Purchases_Click(null!,new RoutedEventArgs());
  var p=Panel();p.Children.Add(Heading(T("Purchase invoices","فاکتورهای خرید")));
  p.Children.Add(Info(T("Purchases increase stock atomically. Accounting journal posting is not yet automatic.","ثبت خرید موجودی را به صورت تراکنشی افزایش می‌دهد؛ سند حسابداری هنوز خودکار نیست.")));
  var suppliers=business.Partners().Where(x=>x.Kind=="Supplier").ToList();var stock=business.Stock();
  var supplier=new ComboBox{ItemsSource=suppliers,DisplayMemberPath="Name",SelectedIndex=suppliers.Count>0?0:-1};p.Children.Add(Info(T("Supplier","تأمین‌کننده")));p.Children.Add(supplier);
  var product=new ComboBox{ItemsSource=stock,DisplayMemberPath="Name",SelectedIndex=stock.Count>0?0:-1};
  var qty=new TextBox{Text="1"};var cost=new TextBox{Text="0.00"};p.Children.Add(Info(T("Product","کالا")));p.Children.Add(product);p.Children.Add(Info(T("Quantity","تعداد")));p.Children.Add(qty);p.Children.Add(Info(T("Unit cost","بهای واحد")));p.Children.Add(cost);
  var draft=new System.Collections.ObjectModel.ObservableCollection<PurchaseDraftLine>();
  var grid=new DataGrid{ItemsSource=draft,IsReadOnly=true,Height=170,Margin=new Thickness(0,12,0,12)};
  var add=new Button{Content=T("Add line","افزودن ردیف")};add.Click+=(_,_)=>{try{if(!CanEdit())return;if(product.SelectedItem is not StockItem item)throw new ArgumentException("Select a product.");if(!int.TryParse(qty.Text,out var count)||count<=0)throw new ArgumentException("Quantity must be positive.");if(!decimal.TryParse(cost.Text,NumberStyles.Number,CultureInfo.CurrentCulture,out var value)&&!decimal.TryParse(cost.Text,NumberStyles.Number,CultureInfo.InvariantCulture,out value))throw new ArgumentException("Invalid unit cost.");if(value<0||decimal.Round(value,2)!=value)throw new ArgumentException("Use a non-negative cost with max 2 decimals.");draft.Add(new PurchaseDraftLine(item.Id,item.Name,count,value));}catch(Exception ex){ShowOperationError(ex, "Purchase", "خطای عملیات");}};p.Children.Add(add);p.Children.Add(grid);
  var remove=new Button{Content=T("Remove selected line","حذف ردیف انتخابی")};remove.Click+=(_,_)=>{if(grid.SelectedItem is PurchaseDraftLine line)draft.Remove(line);};p.Children.Add(remove);
  var save=new Button{Content=T("Post purchase invoice","ثبت فاکتور خرید"),Margin=new Thickness(0,12,0,12)};save.Click+=(_,_)=>{try{if(!CanEdit())return;if(supplier.SelectedItem is not BusinessPartner selected)throw new ArgumentException("Select a supplier.");var id=purchases.Post(selected.Id,draft.Select(x=>new PurchaseService.PurchaseItem(x.StockItemId,x.Quantity,x.UnitCost)).ToList());MessageBox.Show(T($"Purchase invoice #{id} saved.",$"فاکتور خرید شماره {id} ذخیره شد."));Purchases_Click(sender,e);}catch(Exception ex){ShowOperationError(ex, "Purchase validation", "خطای عملیات");}};p.Children.Add(save);
  p.Children.Add(Heading(T("Recent purchases","خریدهای اخیر")));p.Children.Add(new DataGrid{ItemsSource=purchases.Recent().Select(x=>new{x.Id,Supplier=x.Partner?.Name,x.CreatedAtUtc,x.Total,Lines=x.Items.Count}).ToList(),IsReadOnly=true,Height=230});SetWorkspace(p);
 }
 private sealed record PurchaseDraftLine(int StockItemId,string Product,int Quantity,decimal UnitCost){public decimal LineTotal=>Quantity*UnitCost;}
 void Users_Click(object sender,RoutedEventArgs e){currentPage=()=>Users_Click(null!,new RoutedEventArgs());if(currentUser.Role!="Admin")return;var p=Panel();p.Children.Add(Heading("User management"));p.Children.Add(Info("Passwords are stored using PBKDF2-SHA256. Create least-privilege accounts."));p.Children.Add(new DataGrid{ItemsSource=auth.Users().Select(x=>new{x.Username,x.Role,x.IsActive}).ToList(),IsReadOnly=true,Height=220});var username=new TextBox();var password=new PasswordBox{Margin=new Thickness(4),MinHeight=38};var role=new ComboBox{ItemsSource=new[]{"Viewer","Accountant","Admin"},SelectedIndex=0};p.Children.Add(Info("New username"));p.Children.Add(username);p.Children.Add(Info("Password (at least 10 characters)"));p.Children.Add(password);p.Children.Add(Info("Role"));p.Children.Add(role);var add=new Button{Content="Create user"};add.Click+=(_,_)=>{try{auth.CreateUser(username.Text,password.Password,role.SelectedItem?.ToString()??"Viewer");Users_Click(sender,e);}catch(Exception ex){ShowOperationError(ex, "User management", "خطای عملیات");}};p.Children.Add(add);SetWorkspace(p);}
 void Backup_Click(object sender,RoutedEventArgs e){
  try{var dialog=new SaveFileDialog{Title="Back up Ashkan Ledger Pro",Filter="SQLite database (*.db)|*.db",FileName=$"ashkan-ledger-backup-{DateTime.Now:yyyyMMdd-HHmmss}.db"};if(dialog.ShowDialog()!=true)return;
   using var db=new AshkanLedgerPro.Data.LedgerDb();var source=db.Database.GetDbConnection();source.Open();
   using var destination=new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={dialog.FileName}");destination.Open();
   ((Microsoft.Data.Sqlite.SqliteConnection)source).BackupDatabase(destination);
   MessageBox.Show(T("Database backup completed.","پشتیبان‌گیری از پایگاه داده انجام شد."),T("Backup","پشتیبان‌گیری"));
  }catch(Exception ex){ShowOperationError(ex, "Backup failed", "خطای عملیات");}
 }
 static string Csv(string? text) { var value=text??""; if(value.Length>0 && "=+-@".Contains(value[0]))value="'"+value; return "\""+value.Replace("\"","\"\"")+"\""; }
 void Export_Click(object s,RoutedEventArgs e){try{var dialog=new SaveFileDialog{Title="Export journal",Filter="CSV files (*.csv)|*.csv",FileName="ashkan-ledger-journal.csv"};if(dialog.ShowDialog()!=true)return;var sb=new StringBuilder("EntryId,Date,Description,AccountCode,AccountName,Debit,Credit\r\n");foreach(var entry in ledger.Entries())foreach(var line in entry.Lines)sb.AppendLine(string.Join(",",entry.Id,entry.Date.ToString("yyyy-MM-dd"),Csv(entry.Description),Csv(line.Account?.Code),Csv(line.Account?.Name),line.Debit.ToString(CultureInfo.InvariantCulture),line.Credit.ToString(CultureInfo.InvariantCulture)));File.WriteAllText(dialog.FileName,sb.ToString(),new UTF8Encoding(true));MessageBox.Show(T("CSV exported successfully.","فایل CSV با موفقیت ذخیره شد."),T("Export complete","خروجی کامل شد"));}catch(Exception ex){ShowOperationError(ex, "Export failed", "خطای عملیات");}}
}
