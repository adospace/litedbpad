using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDBPad
{
    public class DumpableBsonDocumentCollection : IEnumerable<DumpableBsonDocument>
    {
        IEnumerable<DumpableBsonDocument> _originalQuery;
        public DumpableBsonDocumentCollection(IEnumerable<DumpableBsonDocument> originalQuery)
        {
            if (originalQuery == null) throw new ArgumentNullException(nameof(originalQuery));
            _originalQuery = originalQuery;
        }

        public IEnumerator<DumpableBsonDocument> GetEnumerator()
        {
            return _originalQuery.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _originalQuery.GetEnumerator();
        }
    }
}
