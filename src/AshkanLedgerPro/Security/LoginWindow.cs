using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
namespace AshkanLedgerPro.Security;
public sealed class LoginWindow : Window {
 public AppUser? User {get;private set;}
 public LoginWindow(){bool fa=LanguagePreferences.IsPersian();string L(string en,string persian)=>fa?persian:en;Title=L("Ashkan Ledger Pro | Secure Sign In","اشکان لجر پرو | ورود امن");Width=460;Height=510;ResizeMode=ResizeMode.NoResize;WindowStartupLocation=WindowStartupLocation.CenterScreen;Background=new SolidColorBrush(Color.FromRgb(10,22,39));
 var auth=new AuthService();auth.Initialize();bool setup=!auth.HasUsers();
 FlowDirection=fa?FlowDirection.RightToLeft:FlowDirection.LeftToRight;
 var stack=new StackPanel{Margin=new Thickness(42)};Content=stack;
 stack.Children.Add(new TextBlock{Text="◈ ASHKAN LEDGER PRO",FontSize=25,FontWeight=FontWeights.Bold,Foreground=new SolidColorBrush(Color.FromRgb(38,210,176)),Margin=new Thickness(0,18,0,18)});
 stack.Children.Add(new TextBlock{Text=setup?L("Create the first administrator","ایجاد حساب مدیر اولیه"):L("Sign in to your financial workspace","ورود به محیط مالی"),Foreground=Brushes.White,FontSize=17,Margin=new Thickness(0,0,0,22)});
 TextBox name=new(){MinHeight=40,Margin=new Thickness(0,6,0,14)};PasswordBox password=new(){MinHeight=40,Margin=new Thickness(0,6,0,16)};
 stack.Children.Add(new TextBlock{Text=L("Username","نام کاربری"),Foreground=Brushes.White});stack.Children.Add(name);stack.Children.Add(new TextBlock{Text=L("Password (minimum 10 characters for setup)","رمز عبور (برای ایجاد حساب حداقل ۱۰ نویسه)"),Foreground=Brushes.White});stack.Children.Add(password);
 var message=new TextBlock{Foreground=Brushes.Orange,TextWrapping=TextWrapping.Wrap,Margin=new Thickness(0,12,0,0)};
 var button=new Button{Background=new SolidColorBrush(Color.FromRgb(21,164,143)),Foreground=Brushes.White,FontWeight=FontWeights.SemiBold,Content=setup?L("Create admin & sign in","ایجاد مدیر و ورود"):L("Sign in","ورود"),MinHeight=44};stack.Children.Add(button);stack.Children.Add(message);
 password.KeyDown+=(_,e)=>{if(e.Key==System.Windows.Input.Key.Enter)button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));};
 button.Click+=(_,_)=>{try{if(setup){if(auth.HasUsers())throw new InvalidOperationException("Administrator already exists.");auth.CreateUser(name.Text,password.Password,"Admin");}var u=auth.Login(name.Text.Trim(),password.Password);if(u==null){message.Text=L("Invalid username or password.","نام کاربری یا رمز عبور نادرست است.");return;}User=u;DialogResult=true;}catch(Exception ex){message.Text=ex.Message;}};
 }
}
