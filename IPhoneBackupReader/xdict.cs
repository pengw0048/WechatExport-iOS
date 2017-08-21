//
// Amélioration de la classe dict auto-générée
//

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Serialization;
using System.Diagnostics;


namespace iphonebackupbrowser
{

    // Summary:
    //     un élément d'un <dict>
    public class xdictpair
    {
        public xdictpair(string key, object item)
        {
            this.key = key;
            this.item = item;
        }
        public string key;
        public object item;
    }


    // Summary:
    //     Supports a simple iteration over a nongeneric collection.
    public class xdictEnum : IEnumerator
    {
        private dict _dict;

        // Enumerators are positioned before the first element
        // until the first MoveNext() call.
        int position = -1;

        public xdictEnum(dict list)
        {
            _dict = list;
        }

        public bool MoveNext()
        {
            position++;
            return (position < _dict.key.Length);
        }

        public void Reset()
        {
            position = -1;
        }

        object IEnumerator.Current
        {
            get { return Current; }
        }

        public xdictpair Current
        {
            get { return new xdictpair(_dict.key[position], _dict.Items[position]); }
        }
    }


    // Summary:
    //     Supports a simple iteration over a nongeneric collection.
    public class xdict : IEnumerable
    {
        private dict _dict;


        public xdict(dict d)
        {
            this._dict = d;
        }


        public xdict(object d)
        {
            if (d.GetType() == typeof(dict))
                this._dict = (dict)d;
        }


        // Summary:
        //     Returns an enumerator that iterates through a collection.
        IEnumerator IEnumerable.GetEnumerator()
        {
            return (IEnumerator)GetEnumerator();
        }


        // Summary:
        //     Returns an enumerator that iterates through a collection.
        public xdictEnum GetEnumerator()
        {
            return new xdictEnum(_dict);
        }


        // Summary:
        //     Ouvre une PropertyList dans un TextReader et retourne le <dict> de plus haut niveau
        static public xdict open(TextReader reader)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(plist));

            plist Info = (plist)serializer.Deserialize(reader);

            if (Info.Item.GetType() != typeof(dict))
                return null;

            xdict r = new xdict((dict)Info.Item);

            if (r._dict.key.Length != r._dict.Items.Length)
                return null;

            return r;
        }


        // Summary:
        //     Ouvre une PropertyList dans un fichier et retourne le <dict> de plus haut niveau
        static public xdict open(string filename)
        {
            try
            {
                using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
                {
                    using (TextReader reader = new StreamReader(fs, Encoding.UTF8))
                    {
                        return open(reader);
                    }
                }
            }
            catch (Exception ex)
            {
                // faute de mieux, on ignore...
                Debug.WriteLine("xdict.open exception: {1}", ex.Message);
                return null;
            }
        }


        public object findKey(string key, Type expectedType)
        {
            for (int i = 0; i < _dict.key.Length; ++i)
            {
                if (_dict.key[i] == key)
                {
                    object o = _dict.Items[i];
                    return (o.GetType() == expectedType) ? o : null;
                }
            }

            return null;
        }


        public bool findKey<T>(string key, out T t)
        {
            object o = findKey(key, typeof(T));

            t = (T)o;
            return o != null;
        }


        public Dictionary<string, object> toDictionary()
        {
            if (_dict == null)
                return null;

            Dictionary<string, object> r = new Dictionary<string, object>();

            for (int i = 0; i < _dict.key.Length; ++i)
            {
                r.Add(_dict.key[i], _dict.Items[i]);
            }

            return r;
        }
    }
}
