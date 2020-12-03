using Dicom;
using System;
using System.Collections.Generic;
using System.Text;

namespace dicom
{
    public class DicomDatasetMock : DicomDataset
    {
        /// <summary>
        /// Initializes a new instance of the the Dicom Dataset mock class.
        /// </summary>
        /// <param name="items">An array of DICOM items.</param>
        public DicomDatasetMock(params DicomItem[] items): base((IEnumerable<DicomItem>)items)
        {
           
        }
    }
}
