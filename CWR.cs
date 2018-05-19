using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CWR_processor
{
    class CWR
    {
        private string _filename;
        private DateTime _create_datetime;
        private DateTime _transmission_datetime;

        List<Record> _records;
        List<Transaction> _transactions;
        private CISTables _cis_tables;

        public CWR()
        {
            _records = new List<Record>();
            _transactions = new List<Transaction>();
            _cis_tables = new CISTables();

        }

        public void loadCISTables(string tis_names, string tis_rel) {
            _cis_tables.loadTIS(tis_names,tis_rel);
        }

        public bool loadCWR(string fileName)
        {
            try {
            var lines = File.ReadLines(fileName);
            int line_no = 0;
            int current_group_no = -1;
                foreach (var line in lines)
                {
                    //Add line
                    string tmp = line.Trim();
                    if (tmp.Length > 3)
                    {
                        string type = tmp.Substring(0, 3).ToUpper();
                        if (type == "AGR" ||
                            type == "NWR" ||
                            type == "REV" ||
                            type == "ISW" ||
                            type == "EXC" ||
                            type == "ACK"||
                            type == "SWR"||
                            type == "SWT"||
                            type == "OWR"||
                            type == "OWT")
                        {
                            //Transaction line 
                            Record r = new Record();
                            r.Type = type;
                            r.GroupNo = current_group_no;
                            r.Line = tmp;
                            r.TransmissionNo = Int32.Parse(tmp.Substring(3,8));
                            r.RecordNo = Int32.Parse(tmp.Substring(11, 8));
                            r.LineNo = line_no;
                            _records.Add(r);

                        }
                        else if (type == "GRH")
                        {
                            //Transmission or group line

                            Record r = new Record();
                            r.Type = type;
                            r.GroupNo = Int32.Parse(tmp.Substring(6,5));
                            current_group_no = r.GroupNo;
                            r.Line = tmp;
                            r.LineNo = line_no;
                            _records.Add(r);
                        }
                        else
                        {
                            Record r = new Record();
                            r.Type = type;
                            r.GroupNo = current_group_no;
                            r.Line = tmp;
                            r.LineNo = line_no;
                            _records.Add(r);
                        }
                        line_no++;


                    }
                }
                return true;
            }
            catch { return false; }
            
        }
        
        public void loadNWR()
        {
            List<Record> nwrs = _records.FindAll(o => o.Type == "NWR");

            foreach (Record nwr in nwrs)
            {
                Transaction tr = new Transaction();
                tr.GroupNo = nwr.GroupNo;
                tr.TransactionNo = nwr.TransmissionNo;

                tr.Work.Title = nwr.Line.Substring(19,60);
                tr.Work.ISWC = nwr.Line.Substring(95, 11);
                tr.Work.Workcode = nwr.Line.Substring(81, 14);
                

                _transactions.Add(tr);
            }
        }
        public void loadREV()
        {
            List<Record> revs = _records.FindAll(o => o.Type == "REV");

            foreach (Record rev in revs)
            {
                Transaction tr = new Transaction();
                tr.GroupNo = rev.GroupNo;
                tr.TransactionNo = rev.TransmissionNo;

                tr.Work.Title = rev.Line.Substring(19, 60);
                tr.Work.ISWC = rev.Line.Substring(95, 11);
                tr.Work.Workcode = rev.Line.Substring(81, 14);

                _transactions.Add(tr);
            }
        }

        public void updateTransactions()
        {
            foreach (Transaction tr in _transactions) updateWorkSplits(tr);
        }

        public Transaction updateWorkSplits(Transaction tr)
        {

            tr = updateSWR(tr);
            tr = updateOWR(tr);

            return tr;
        
        }


        private Transaction updateSWR(Transaction tr)
        {

            //Controlled writers - SWR

            int ref_number = 1;
            List<Record> revs = _records.FindAll(o => o.GroupNo == tr.GroupNo && o.TransmissionNo == tr.TransactionNo && o.Type == "SWR");


            foreach (Record r in revs)
            {
                IPName writer = new IPName();
                writer.LastName = r.Line.Substring(28, 45);
                writer.FirstName = r.Line.Substring(73, 30);
                writer.IPINameNo = r.Line.Substring(115, 11);
                writer.NaturalPerson = true;
                writer.RefNumber = r.Line.Substring(19, 9);
                


                Shareholder sh = new Shareholder();
                sh.InterestedParty = writer;
                sh.Controlled = true;
                sh.Level = 1;
                sh.RefNo = ref_number;
                //sh.

                sh.Role = r.Line.Substring(104, 2);
                sh.POwn = Decimal.Parse(r.Line.Substring(129, 5)) / 100;
                sh.MOwn = Decimal.Parse(r.Line.Substring(137, 5)) / 100;
                sh.SOwn = Decimal.Parse(r.Line.Substring(145, 5)) / 100;

                sh.PSoc = Int32.Parse(r.Line.Substring(126, 3));
                sh.MSoc = Int32.Parse(r.Line.Substring(134, 3));
                sh.SSoc = Int32.Parse(r.Line.Substring(142, 3));

                TIS territory_controll = new TIS();
                territory_controll.TISN = 2136;
                territory_controll.Name = "WORLD";
                territory_controll.Inluded = true;

                List<TIS> tisList = new List<TIS>();
                tisList.Add(territory_controll);

                tr.Work.addShareholderList(sh, tisList, this._cis_tables);

                int line = r.LineNo;


            }



            return tr;

        }

        private Transaction updateOWR(Transaction tr)
        {

            //Not-controlled writers - OWR

            int ref_number = 1;
            List<Record> revs = _records.FindAll(o => o.GroupNo == tr.GroupNo && o.TransmissionNo == tr.TransactionNo && o.Type == "OWR");


            foreach (Record r in revs)
            {
                IPName writer = new IPName();
                writer.LastName = r.Line.Substring(28, 45);
                writer.FirstName = r.Line.Substring(73, 30);
                writer.IPINameNo = r.Line.Substring(115, 11);
                writer.NaturalPerson = true;
                writer.RefNumber = r.Line.Substring(19, 9);



                Shareholder sh = new Shareholder();
                sh.InterestedParty = writer;
                sh.Controlled = false;
                sh.Level = 1;
                sh.RefNo = ref_number;

                sh.Role = r.Line.Substring(104, 2);
                sh.POwn = Decimal.Parse(r.Line.Substring(129, 5)) / 100;
                sh.MOwn = Decimal.Parse(r.Line.Substring(137, 5)) / 100;
                sh.SOwn = Decimal.Parse(r.Line.Substring(145, 5)) / 100;

                sh.PSoc = Int32.Parse(r.Line.Substring(126, 3));
                sh.MSoc = Int32.Parse(r.Line.Substring(134, 3));
                sh.SSoc = Int32.Parse(r.Line.Substring(142, 3));

                TIS territory_controll = new TIS();
                territory_controll.TISN = 2136;
                territory_controll.Name = "WORLD";
                territory_controll.Inluded = true;

                List<TIS> tisList = new List<TIS>();
                tisList.Add(territory_controll);

                tr.Work.addShareholderList(sh, tisList, this._cis_tables);

                int line = r.LineNo;


            }



            return tr;

        }

        public void printWorks(TIS territory)
        {
            List<TIS> territoryList = new List<TIS>();
            territoryList.Add(territory);
            foreach (Transaction tr in _transactions.FindAll(o => o.Work.SplitOptions.Exists(t => this._cis_tables.TISValidList(t.Territories, territoryList))))
            {
                Console.WriteLine(tr.Work.Workcode+"\t"+tr.Work.Title);
                
                foreach(Shareholder sh in tr.Work.SplitOptions[0].Shareholders) {
                    Console.WriteLine(sh.InterestedParty.LastName + " " + sh.InterestedParty.FirstName + "\t" + sh.InterestedParty.IPINameNo + "\t" + sh.Role +"\t"+sh.Controlled+ "\t" + sh.POwn + "\t" + sh.PCol + "\t" + sh.MOwn + "\t" + sh.MCol);
                }

                Console.WriteLine();
                Console.WriteLine();
                Console.WriteLine();
            }
        }
    }

    class Record
    {
        private string _type;
        private int _group_no;
        private int _transmission_no;
        private int _record_no;
        private int _line_no;
        private string _line;

        public string Line
        {
            get { return _line; }
            set
            {
                string tmp = value.Trim();
                _line = value;
                
            }
        }

        public int LineNo
        {
            get { return _line_no; }
            set { _line_no = value; }
        }

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public int GroupNo
        {
            get { return _group_no; }
            set { _group_no = value; }
        }
        public int TransmissionNo
        {
            get { return _transmission_no; }
            set { _transmission_no = value; }
        }
        public int RecordNo
        {
            get { return _record_no; }
            set { _record_no = value; }
        }
    }

    class IPName
    {
        private string _ip_ref_number;

        private bool _natural_person;
        private string _name_type;

        private string _last_name;
        private string _first_names;

        private string _ipi_name_number;
        private string _ipi_base_number;

        public string RefNumber
        {
            get { return _ip_ref_number; }
            set { _ip_ref_number = value; }
        }
        public string LastName
        {
            get { return _last_name; }
            set { _last_name = value.Trim(); }
        }
        public string FirstName
        {
            get { return _first_names; }
            set { _first_names = value.Trim(); }
        }
        public string IPINameNo
        {
            get { return _ipi_name_number; }
            set { _ipi_name_number = value.Trim(); }
        }
        public bool NaturalPerson
        {
            get { return _natural_person; }
            set { _natural_person = value; }
        }
        public string Type
        {
            get { return _name_type; }
            set { _name_type = value.Trim(); }
        }
        public string IPBaseNo
        {
            get { return _ipi_base_number; }
            set { _ipi_base_number = value.Trim(); }
        }

    }

    class Shareholder
    {
        private IPName _interested_party;
        private string _role;


        private Decimal _m_ownership;
        private Decimal _p_ownership;
        private Decimal _s_ownership;

        private Decimal _m_collection;
        private Decimal _p_collection;
        private Decimal _s_collection;

        private int _m_society;
        private int _p_society;
        private int _s_society;

        private int _ref_number;
        private int _level;
        private int _parent_ref_number;
        bool _controlled;

        
        public IPName InterestedParty
        {
            get { return _interested_party; }
            set { _interested_party = value; }
        }
        public string Role
        {
            get { return _role; }
            set { _role = value.Trim(); }
        }
        public Decimal MOwn
        {
            get { return _m_ownership; }
            set { _m_ownership = Math.Round(value, 2); }
        }
        public Decimal POwn
        {
            get { return _p_ownership; }
            set { _p_ownership = Math.Round(value, 2); }
        }
        public Decimal SOwn
        {
            get { return _s_ownership; }
            set { _s_ownership = Math.Round(value, 2); }
        }

        public Decimal MCol
        {
            get { return _m_collection; }
            set { _m_collection = Math.Round(value, 2); }
        }
        public Decimal PCol
        {
            get { return _p_collection; }
            set { _p_collection = Math.Round(value, 2); }
        }
        public Decimal SCol
        {
            get { return _s_collection; }
            set { _s_collection = Math.Round(value, 2); }
        }

        public int MSoc
        {
            get { return _m_society; }
            set { _m_society = value; }
        }
        public int PSoc
        {
            get { return _p_society; }
            set { _p_society = value; }
        }
        public int SSoc
        {
            get { return _s_society; }
            set { _s_society = value; }
        }

        public int RefNo
        {
            get { return _ref_number; }
            set { _ref_number = value; }
        }
        public int Level
        {
            get { return _level; }
            set { _level = value; }
        }
        public int ParentRefNo
        {
            get { return _parent_ref_number; }
            set { _parent_ref_number = value; }
        }
        public bool Controlled
        {
            get { return _controlled; }
            set { _controlled = value; }
        }

    }

    class RightSplitOption
    {
        private List<Shareholder> _shareholders;
        private List<TIS> _territories;

        public RightSplitOption() {
            _shareholders = new List<Shareholder>();
            _territories = new List<TIS>();
        }

        public List<Shareholder> Shareholders {
            get { return _shareholders; }
        }

        public void addShareholder(Shareholder sh)
        {
            _shareholders.Add(sh);
        }

        public List<TIS> Territories
        {
            get { return _territories; }
        }

        public void addTIS(TIS tis)
        {
            _territories.Add(tis);
        }


        public bool tisValid(int tisn)
        {
            return false;
        }
    }

    class TIS
    {
        private int _tis_no;
        private bool _include;
        private string _tis_name;
        private string _tis_code_short;
        private string _tis_code_long;
        private string _type;

        public int TISN
        {
            get { return _tis_no; }
            set { _tis_no = value; }
        }

        public string Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public bool Inluded
        {
            get { return _include; }
            set { _include = value; }
        }

        public string Name
        {
            get { return _tis_name; }
            set { _tis_name = value; }
        }

        public string ShortCode
        {
            get { return _tis_code_short; }
            set { _tis_code_short = value; }
        }

        public string LongCode
        {
            get { return _tis_code_long; }
            set { _tis_code_long = value; }
        }


    }

    class Work
    {
        public bool _rfk;

        private string _title;
        private string _workcode;
        private string _iswc;

        private List<IPName> _creators;

        private List<RightSplitOption> _split_options;

        public Work () {
        
        _split_options = new List<RightSplitOption>();
        RightSplitOption rso = new RightSplitOption();


            //Creating +2WL split option
        TIS tis = new TIS();
        tis.TISN = 2136;
        tis.ShortCode = "2WL";

        tis.Inluded = true;
        rso.addTIS(tis);
        _split_options.Add(rso);

        }

        public string Title
        {
            get { return _title;  }
            set { _title = value.Trim(); }
        }

        public string Workcode
        {
            get { return _workcode; }
            set { _workcode = value.Trim(); }
        }

        public string ISWC
        {
            get { return _iswc; }
            set { _iswc = value.Trim(); }
        }

        public List<RightSplitOption> SplitOptions {
            get { return _split_options; }
        }

        public void addSplitOption(RightSplitOption rso)
        {
            _split_options.Add(rso);
        }

        
        public void addShareholderList(Shareholder sh, List<TIS> tisList, CISTables validator) {


            RightSplitOption validSplitOption = SplitOptions.Find(o => validator.TISValidList(o.Territories, tisList) && validator.TISValidList(tisList, o.Territories));


            if (validSplitOption != null)
            {
                validSplitOption.addShareholder(sh);
            }
            else
            {
                RightSplitOption commonTis = new RightSplitOption();
                RightSplitOption notCommonBase = new RightSplitOption();
                RightSplitOption notCommonInclusion = new RightSplitOption();


                //Console.WriteLine("Error");
            }

        }
    }

    class Transaction
    {
        private Work _work;
        private int _group_no;
        private int _transaction_no;

        public Transaction()
        {
            _work = new Work();
        }

        public Work Work
        {
            set { _work = value; }
            get { return _work; }
        }

        public int GroupNo
        {
            get { return _group_no; }
            set { _group_no = value; }
        }

        public int TransactionNo
        {
            get { return _transaction_no; }
            set { _transaction_no = value; }
        }
    }

    class CISTables {

        List<KeyValuePair<TIS, TIS>> _tis_relation_database;
        List<TIS> _tis_names;
        public CISTables()
        {
            _tis_relation_database = new List<KeyValuePair<TIS, TIS>>();
            _tis_names = new List<TIS>();

        }

        public void loadTIS(string tisNames, string tisRelations)
        {
            TISNames(tisNames);
            TISHierarhy(tisRelations);
        }

        private void TISNames(string fileName)
        {
             var lines = File.ReadLines(fileName);

             foreach (var line in lines)
             {
                 string[] columns = line.Split('\t');

                 TIS territory = new TIS();
                 territory.TISN = Int32.Parse(columns[0]);
                 territory.Inluded = true;
                 territory.Type = columns[1];
                 territory.ShortCode =  columns[2];
                 territory.LongCode = columns[3];
                 territory.Name = columns[4];
                 _tis_names.Add(territory);
             }
        }

        private void TISHierarhy(string fileName)
        {
            var lines = File.ReadLines(fileName);

            foreach (var line in lines)
            {
                string[] columns = line.Split('\t');
                TIS parent = _tis_names.Find(o => o.TISN == Int32.Parse(columns[0]));
                //parent.TISN = Int32.Parse(columns[0]);

                TIS child = _tis_names.Find(o => o.TISN == Int32.Parse(columns[1]));
                //child.TISN = Int32.Parse(columns[1]);

                KeyValuePair<TIS, TIS> t = new KeyValuePair<TIS, TIS>(parent, child);
               
                _tis_relation_database.Add(t);

            }
        }

        public bool TISValid(TIS parent, TIS child)
        {
            bool valid = true;


                if(child.Inluded) {
                    valid = valid && (this._tis_relation_database.Exists(t => t.Key.TISN == child.TISN && t.Value.TISN == parent.TISN) || parent.TISN == child.TISN);
                }
                else
                {
                    valid = valid && !(this._tis_relation_database.Exists(t => t.Key.TISN == child.TISN && t.Value.TISN == parent.TISN) || parent.TISN == child.TISN);
                }
            
            return valid;
        }

        public bool TISValidList(List<TIS> parentList, List<TIS> childList)
        {
            bool valid = true;
            foreach (TIS parent in parentList)
            {
                foreach (TIS child in childList)
                {
                    valid = valid && this.TISValid(parent, child);
                }
            }
            
            return valid;
        }

        //public void TIS


        public List<TIS> TISnames
        {
            get { return _tis_names; }
        }
    }
}
