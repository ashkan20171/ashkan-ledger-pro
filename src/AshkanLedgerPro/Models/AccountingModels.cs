namespace AshkanLedgerPro.Models;
public class Account { public int Id {get;set;} public string Code {get;set;} = ""; public string Name {get;set;} = ""; public string Type {get;set;} = "Asset"; }
public class JournalEntry { public int Id {get;set;} public DateTime Date {get;set;}=DateTime.Today; public string Description {get;set;}=""; public bool Posted {get;set;} public List<JournalLine> Lines {get;set;}=new(); }
public class JournalLine { public int Id {get;set;} public int JournalEntryId {get;set;} public int AccountId {get;set;} public Account? Account {get;set;} public decimal Debit {get;set;} public decimal Credit {get;set;} }
