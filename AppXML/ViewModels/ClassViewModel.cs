﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Caliburn.Micro;
using AppXML.Models;
using System.Collections.ObjectModel;
using System.Xml;
using System.Windows.Controls;
using AppXML.Data;
using System.Dynamic;
using System.Windows;

namespace AppXML.ViewModels
{
    public class ClassViewModel:PropertyChangedBase
    {

        private ClassViewModel _resumeClassViewModel;

        public ClassViewModel ResumeClass { get { return _resumeClassViewModel; } set { _resumeClassViewModel = value; } }

        private ChoiceViewModel _choice;
        private ObservableCollection<IntegerViewModel> _items;
        private ObservableCollection<MultiValuedViewModel> _multis;
        private ObservableCollection<BranchViewModel> _branches;
        private ObservableCollection<XMLNodeRefViewModel> _XMLNODE;
        private string _resume;
        private string _className;
        private WindowClassViewModel _wclvm;
        private XmlDocument _doc;
        public XmlNode resume;

        //faltan los branches pero estan sin crear BranchViewModel y BranchView
        public ClassViewModel(string clasName, XmlDocument doc)
        {
            _doc = doc;
            _className = clasName;
            XmlNode node = CNode.definitions[clasName];
            if (node.Attributes["Window"] != null)
            {
                _resume = "Press the button to open the form";
                _wclvm = new WindowClassViewModel(clasName, this,_doc);
               
            }
            else
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name == "CHOICE")
                    {
                        _choice = new ChoiceViewModel(child,_doc);
                    }
                    else if (child.Name.EndsWith("VALUE"))
                    {
                        if (_items == null)
                            _items = new ObservableCollection<IntegerViewModel>();
                        CIntegerValue civ = CNode.getInstance(child) as CIntegerValue;
                        IntegerViewModel ivw = new IntegerViewModel(child.Attributes["Name"].Value, civ,_doc);
                        _items.Add(ivw);

                    }
                    else if (child.Name == "MULTI-VALUED")
                    {
                        //to do: añadir los multis a su lista y añadirlo en el xaml
                        if (_multis == null)
                            _multis = new ObservableCollection<MultiValuedViewModel>();
                        bool isOptional = false;
                        if (child.Attributes["Optional"] != null)
                            isOptional = Convert.ToBoolean(child.Attributes["Optional"].Value);
                        string comment = null;
                        if (child.Attributes["Comment"] != null)
                            comment = child.Attributes["Comment"].Value;
                        MultiValuedViewModel mvvm = new MultiValuedViewModel(child.Attributes["Name"].Value, child.Attributes["Class"].Value,comment,isOptional,doc);
                        _multis.Add(mvvm);
                    }
                    else if (child.Name == "BRANCH")
                    {

                        if (_branches == null)
                            _branches = new ObservableCollection<BranchViewModel>();
                        bool isOptional=false;
                        if (child.Attributes["Optional"]!=null)
                            isOptional = Convert.ToBoolean(child.Attributes["Optional"].Value);
                        string comment = null;
                        if (child.Attributes["Comment"] != null)
                            comment = child.Attributes["Comment"].Value;
                        BranchViewModel bvm = new BranchViewModel(child.Attributes["Name"].Value, child.Attributes["Class"].Value,comment,isOptional,_doc);
                        _branches.Add(bvm);
                    }
                    else if (child.Name == "XML-NODE-REF")
                    {
                        string label = child.Attributes["Name"].Value;
                        string action = child.Attributes["HangingFrom"].Value;
                        string xmlfile = child.Attributes["XMLFile"].Value;
                        if (_XMLNODE == null)
                            _XMLNODE = new ObservableCollection<XMLNodeRefViewModel>();
                        this._XMLNODE.Add(new XMLNodeRefViewModel(label, xmlfile, action,_doc));
                    }
                    else if (child.Name == "RESUME")
                    {
                        resume = child;

                    }
                }
            }


        }

        public ClassViewModel(string clasName, Boolean ignoreWindow, XmlDocument doc)
        {
            _doc = doc;
            _className = clasName;
            XmlNode node = CNode.definitions[clasName];
            if (ignoreWindow && node.Attributes["Window"] != null)
            {
                _resume = "Press the button to open the form";
                _wclvm = new WindowClassViewModel(clasName, this,_doc);
            }
            else
            {
                foreach (XmlNode child in node.ChildNodes)
                {
                    if (child.Name == "CHOICE")
                    {
                        _choice = new ChoiceViewModel(child,_doc);
                    }
                    else if (child.Name.EndsWith("VALUE"))
                    {
                        if (_items == null)
                            _items = new ObservableCollection<IntegerViewModel>();
                        CIntegerValue civ = CNode.getInstance(child) as CIntegerValue;
                        IntegerViewModel ivw = new IntegerViewModel(child.Attributes["Name"].Value, civ,_doc);
                        _items.Add(ivw);

                    }
                    else if (child.Name == "MULTI-VALUED")
                    {
                        //to do: añadir los multis a su lista y añadirlo en el xaml
                        if (_multis == null)
                            _multis = new ObservableCollection<MultiValuedViewModel>();
                        bool isOptional = false;
                        if (child.Attributes["Optional"] != null)
                            isOptional = Convert.ToBoolean(child.Attributes["Optional"].Value);
                        string comment = null;
                        if (child.Attributes["Comment"] != null)
                            comment = child.Attributes["Comment"].Value;
                        MultiValuedViewModel mvvm = new MultiValuedViewModel(child.Attributes["Name"].Value, child.Attributes["Class"].Value, comment, isOptional,doc); _multis.Add(mvvm);
                    }
                    else if (child.Name == "BRANCH")
                    {

                        if (_branches == null)
                            _branches = new ObservableCollection<BranchViewModel>();
                        bool isOptional = false;
                        if (child.Attributes["Optional"] != null)
                            isOptional = Convert.ToBoolean(child.Attributes["Optional"].Value);
                        string comment = null;
                        if (child.Attributes["Comment"] != null)
                            comment = child.Attributes["Comment"].Value;
                        BranchViewModel bvm = new BranchViewModel(child.Attributes["Name"].Value, child.Attributes["Class"].Value, comment, isOptional,_doc);
                        _branches.Add(bvm);
                    }
                    else if (child.Name == "XML-NODE-REF")
                    {
                        string label = child.Attributes["Name"].Value;
                        string action = child.Attributes["HangingFrom"].Value;
                        string xmlfile = child.Attributes["XMLFile"].Value;
                        if (_XMLNODE == null)
                            _XMLNODE = new ObservableCollection<XMLNodeRefViewModel>();
                        this._XMLNODE.Add(new XMLNodeRefViewModel(label, xmlfile, action,_doc));
                    }
                    else if(child.Name == "RESUME")
                    {
                        resume = child;

                    }
                }
            }


        }

        public string ClassViewVisible { get { if (_resume != null)return "Hidden"; else return "Visible"; } set { } }
        public string ResumeVisible { get { if (_resume == null)return "Hidden"; else return "Visible"; } set { } }
        public string ItemsVisible { get { if(Items == null || _resume!=null )return "Hidden";else return "Visible"; } set { } }
        public string ChoiceVisible { get { if (Choice == null || _resume != null)return "Hidden"; else return "Visible"; } set { } }
        public string BranchesVisible { get { if (Branches == null || _resume != null)return "Hidden"; else return "Visible"; } set { } }
        public string MultisVisible { get { if (Multis == null || _resume != null)return "Hidden"; else return "Visible"; } set { } }
        public string XMLNodeVisible { get { if (_XMLNODE == null || _resume != null)return "Hidden"; else return "Visible"; } set { } }
        public ChoiceViewModel Choice { get { return _choice; } set { _choice = value; } }
        public ObservableCollection<IntegerViewModel> Items { get { return _items; } set { } }
        public ObservableCollection<MultiValuedViewModel> Multis { get { return _multis; } set { } }
        public ObservableCollection<BranchViewModel> Branches { get { return _branches; } set { } }
        public ObservableCollection<XMLNodeRefViewModel> XMLNODE { get { return _XMLNODE; } set { } }
        public string Resume { get { return _resume; } set { _resume = value; NotifyOfPropertyChange(() => Resume); } }

        public void removeViews()
        {
            if(_XMLNODE!=null)
                CApp.removeViews(_XMLNODE.ToList());
            if (_branches != null)
            {
                foreach(BranchViewModel branch in _branches)
                {
                    branch.removeViews();
                }
            }
            if(_choice!=null)
            {
                _choice.removeViews();
            }
            if(_multis!=null)
            {
                foreach(MultiValuedViewModel multi in _multis)
                {
                    multi.removeViews();
                }
            }
                
        }
        public void OpenForm()
        {
            

           WindowManager windowManager = new WindowManager();
           windowManager.ShowDialog(this._wclvm);
           

            
        }
        public bool validate()
        {
            if(_wclvm==null)
            {
                if (_branches != null)
                {
                    foreach (BranchViewModel item in _branches)
                    {
                        if (!item.validate())
                            return false;
                    }
                }
                if (_items != null)
                {
                    foreach (IntegerViewModel item in _items)
                    {
                        if (!item.validateIntegerViewModel())
                            return false;
                    }
                }
                if (_multis != null)
                {
                    foreach (MultiValuedViewModel item in _multis)
                    {
                        if (!item.validate())
                            return false;
                    }
                }
                if (_choice != null)
                {
                    return _choice.validate();
                }
                if (_XMLNODE != null)
                {
                    foreach (XMLNodeRefViewModel item in _XMLNODE)
                    {
                        if (!item.validate())
                            return false;
                    }
                }
                
                return true;
            }
           
            else
            {
                return ResumeClass.validate();
            }
            
        }

        public XmlNode getXmlNode()
        {
            XmlNode nodo = _doc.CreateElement(_className);
            if (_branches != null)
            {
                foreach (BranchViewModel item in _branches)
                {
                    nodo.AppendChild(item.getXmlNode());
                }
            }
            if (_items != null)
            {
                foreach (IntegerViewModel item in _items)
                {
                    nodo.AppendChild(item.getXmlNode());
                }
            }
            
            if (_multis != null)
            {
                foreach (MultiValuedViewModel item in _multis)
                {
                    List<XmlNode> nodes = item.getXmlNode();
                    foreach (XmlNode node in nodes)
                        nodo.AppendChild(node);
                }
            }
            
            if (_choice != null)
            {
                nodo.AppendChild(_choice.getXmlNode());
            }
            
            if (_XMLNODE != null)
            {
                foreach (XMLNodeRefViewModel item in _XMLNODE)
                {
                    nodo.AppendChild(item.getXmlNode());
                }
            }
              
           
            return nodo;
        }
    }
}
