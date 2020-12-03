﻿// Copyright (c) 2012-2020 fo-dicom contributors.
// Licensed under the Microsoft Public License (MS-PL).

using dicom;
using Dicom.Helpers;
using System;
using System.IO;
using System.Linq;
using Xunit;

namespace Dicom
{
    [Collection("General")]
    public class DicomValidationTest
    {

        #region Unit tests

        [Fact]
        public void DicomValidation_AddValidData()
        {
            var ds = new DicomDataset();
            var validUid = "1.2.315.6666.0.8965.19187632.1";
            ds.Add(DicomTag.StudyInstanceUID, validUid);
            Assert.Equal(validUid, ds.GetSingleValue<string>(DicomTag.StudyInstanceUID));
        }

        [Fact]
        public void DicomValidation_AddInvalidData()
        {
            var ds = new DicomDataset();
            var invalidUid = "1.2.315.6666.008965..19187632.1";
            // trying to add this invalidUid should throw exception
            Assert.Throws<DicomValidationException>(() => ds.Add(DicomTag.StudyInstanceUID, invalidUid));

            ds.AutoValidate = false;
            // if AutoValidate is turned off, the invalidUid should be able to be added
            ds.Add(DicomTag.StudyInstanceUID, invalidUid);
            Assert.Equal(invalidUid, ds.GetSingleValue<string>(DicomTag.StudyInstanceUID));

            var tmpFile = Path.GetTempFileName();
            ds.Add(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage);
            ds.Add(DicomTag.SOPInstanceUID, DicomUIDGenerator.GenerateNew().UID);
            // save this invalid dicomdataset
            (new DicomFile(ds)).Save(tmpFile);

            // reading of this invalid dicomdataset should be possible
            var dsFile = DicomFile.Open(tmpFile);
            Assert.Equal(invalidUid, dsFile.Dataset.GetSingleValue<string>(DicomTag.StudyInstanceUID));

            // but the validation should still work
            Assert.Throws<DicomValidationException>(() => dsFile.Dataset.Validate());
            IOHelper.DeleteIfExists(tmpFile);
        }

        [Fact]
        public void DicomValidation_ValidateUID()
        {
            var ds = new DicomDataset();
            var validUid = "1.2.315.6666.0.0.0.8965.19187632.1";
            ds.Add(DicomTag.StudyInstanceUID, validUid);
            Assert.Equal(validUid, ds.GetSingleValue<string>(DicomTag.StudyInstanceUID));

            var tooLongUid = validUid + "." + validUid;
            var ex = Assert.ThrowsAny<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.StudyInstanceUID, tooLongUid));
            Assert.Contains("length", ex.Message);

            var leadingZeroUid = validUid + ".03";
            var ex2 = Assert.ThrowsAny<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.SeriesInstanceUID, leadingZeroUid));
            Assert.Contains("leading zero", ex2.Message);
        }

        [Fact]
        public void DicomValidation_ValidateCodeString()
        {
            var ds = new DicomDataset();
            var validAETitle = "HUGO1";
            ds.Add(DicomTag.ReferencedFileID, validAETitle);

            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ReferencedFileID, "Hugo1"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ReferencedFileID, "HUGO-1"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ReferencedFileID, "HUGOHUGOHUGOHUGO1"));
        }


        [Fact]
        public void DicomValidation_ValidateCodeStringWithGlobalSuppression()
        {
            DicomValidation.AutoValidation = false;
            var ds = new DicomDataset();
            var validAETitle = "HUGO1";
            ds.Add(DicomTag.ReferencedFileID, validAETitle);

            Assert.Null(Record.Exception(() => ds.AddOrUpdate(DicomTag.ReferencedFileID, "Hugo1")));
            Assert.Null(Record.Exception(() => ds.AddOrUpdate(DicomTag.ReferencedFileID, "HUGO-1")));
            Assert.Null(Record.Exception(() => ds.AddOrUpdate(DicomTag.ReferencedFileID, "HUGOHUGOHUGOHUGO1")));
            DicomValidation.AutoValidation = true;
        }


        [Fact]
        public void AddInvalidUIDMultiplicity()
        {
            Assert.Throws<DicomValidationException>(() =>
            {
                var ds = new DicomDataset();
                ds.Add(DicomTag.SeriesInstanceUID, "1.2.3\\3.4.5");
            });

            Assert.Throws<DicomValidationException>(() =>
            {
                var ds = new DicomDataset();
                ds.Add(DicomTag.SeriesInstanceUID, "1.2.3", "2.3.4");
            });

            Assert.Throws<DicomValidationException>(() =>
            {
                var ds = new DicomDataset();
                ds.Add(new DicomUniqueIdentifier(DicomTag.SeriesInstanceUID, "1.2.3", "3.4.5"));
            });
        }

        [Fact()]
        public void ValidationAllowESCInSeriesDescriptionTag()
        {
            var ex = Record.Exception(() =>
            {
                var ds = new DicomDataset();
                ds.Add(new DicomLongString(DicomTag.SeriesDescription, "A ESC value: \u001b"));
            });
            Assert.Null(ex);
        }


        [Fact()]
        public void AddInvalidUIDMultiplicityWithGlobalSuppression()
        {
            DicomValidation.PerformValidation = false;
            Assert.Null(Record.Exception(() =>
            {
                var ds = new DicomDataset();
                ds.Add(DicomTag.SeriesInstanceUID, "1.2.3\\3.4.5");
            }));

            Assert.Null(Record.Exception(() =>
            {
                var ds = new DicomDataset();
                ds.Add(DicomTag.SeriesInstanceUID, "1.2.3", "2.3.4");
            }));

            Assert.Null(Record.Exception(() =>
            {
                var ds = new DicomDataset();
                ds.Add(new DicomUniqueIdentifier(DicomTag.SeriesInstanceUID, "1.2.3", "3.4.5"));
            }));
            DicomValidation.PerformValidation = true;
        }

        [Fact()]
        public void ValidateLongText()
        {

            #region long text
            var str17170CharText = @"Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. 

Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. 

Nam liber tempor cum soluta nobis eleifend option congue nihil imperdiet doming id quod mazim placerat facer possim assum. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis. 

At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, At accusam aliquyam diam diam dolore dolores duo eirmod eos erat, et nonumy sed tempor et et invidunt justo labore Stet clita ea et gubergren, kasd magna no rebum. sanctus sea sed takimata ut vero voluptua. est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat. 

Consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. 

Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. 

Nam liber tempor cum soluta nobis eleifend option congue nihil imperdiet doming id quod mazim placerat facer possim assum. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis. 

At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, At accusam aliquyam diam diam dolore dolores duo eirmod eos erat, et nonumy sed tempor et et invidunt justo labore Stet clita ea et gubergren, kasd magna no rebum. sanctus sea sed takimata ut vero voluptua. est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat. 

Consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. 

Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. 

Nam liber tempor cum soluta nobis eleifend option congue nihil imperdiet doming id quod mazim placerat facer possim assum. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis. 

At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, At accusam aliquyam diam diam dolore dolores duo eirmod eos erat, et nonumy sed tempor et et invidunt justo labore Stet clita ea et gubergren, kasd magna no rebum. sanctus sea sed takimata ut vero voluptua. est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat. 

Consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. 

Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. 

Nam liber tempor cum soluta nobis eleifend option congue nihil imperdiet doming id quod mazim placerat facer possim assum. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nisl ut aliquip ex ea commodo consequat. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis. 

At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, At accusam aliquyam diam diam dolore dolores duo eirmod eos erat, et nonumy sed tempor et et invidunt justo labore Stet clita ea et gubergren, kasd magna no rebum. sanctus sea sed takimata ut vero voluptua. est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat. 

Consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. 

Duis autem vel eum iriure dolor in hendrerit in vulputate velit esse molestie consequat, vel illum dolore eu feugiat nulla facilisis at vero eros et accumsan et iusto odio dignissim qui blandit praesent luptatum zzril delenit augue duis dolore te feugait nulla facilisi. Lorem ipsum dolor sit amet, consectetuer adipiscing elit, sed diam nonummy nibh euismod tincidunt ut laoreet dolore magna aliquam erat volutpat. 

Ut wisi enim ad minim veniam, quis nostrud exerci tation ullamcorper suscipit lobortis nis";
            #endregion

            var ex = Record.Exception(() =>
            {
                //Test Dataset
                DicomDataset TestDataset = new DicomDataset
                {
                    { DicomVR.AE, DicomTag.ScheduledStationAETitle, "FEMTO_02" },
                    { DicomVR.DA, DicomTag.ScheduledProcedureStepStartDate, "20191030" },
                    { DicomVR.TM, DicomTag.ScheduledProcedureStepStartTime, "233005" },
                    { DicomVR.DA, DicomTag.ScheduledProcedureStepEndDate, "20191031" },
                    { DicomVR.TM, DicomTag.ScheduledProcedureStepEndTime, "012433" },
                    { DicomVR.LT, DicomTag.CommentsOnTheScheduledProcedureStep, str17170CharText },
                    { DicomVR.CS, DicomTag.Modality, "LTPA" },
                    { DicomVR.PN, DicomTag.ScheduledPerformingPhysicianName, "Mustermann^Max^Heinrich^Prof. Dr.^, III." },
                    { DicomVR.LO, DicomTag.ScheduledProcedureStepDescription, "Dummy Description Scheduled Procedure Step" },
                    { DicomVR.SH, DicomTag.ScheduledStationName, "OP 2" },
                    { DicomVR.SH, DicomTag.ScheduledProcedureStepLocation, "Room 1.O2.7" },
                    { DicomVR.LO, DicomTag.PreMedication, "Ofloxacin OR Zymar; Cyclopen. dil- drop; Prednisolone Ace. 1%" },
                    { DicomVR.SH, DicomTag.ScheduledProcedureStepID, "SPS000001" },
                    { DicomVR.SH, DicomTag.ScheduledProcedureStepStatus, "SCHEDULED" }
                };
            });
            Assert.NotNull(ex);
        }

        [Fact]
        public void DicomValidation_ValidateDA()
        {
            var ds =
                new DicomDatasetMock(
                    new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage),
                    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));

            ds.Add(DicomTag.ScheduledProcedureStepStartDate, "20191031");
            ds.Add(DicomTag.ScheduledProcedureStepEndDate, "20191030-");
            ds.Add(DicomTag.PerformedProcedureStepStartDate, "-20191228");
            ds.Add(DicomTag.PerformedProcedureStepEndDate, "20190101-20200101");

            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, "19970101-");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepEndDate, "-19701231");
            ds.AddOrUpdate(DicomTag.PerformedProcedureStepStartDate, "20190101-20200101");
            ds.AddOrUpdate(DicomTag.PerformedProcedureStepEndDate, "20190101-20200101");

            var currentDate = System.DateTime.Now.ToString("yyyyMMdd");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, currentDate);

            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDate, "20191031--"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepEndDate, "-20191031-"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.PerformedProcedureStepStartDate, "201911"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.PerformedProcedureStepStartDate, "20193101"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.PerformedProcedureStepStartDate, "20191232"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.PerformedProcedureStepEndDate, "31122008"));
        }

        [Fact]
        public void DicomValidation_ValidateDT()
        {
            var ds =
                new DicomDatasetMock(
                    new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage),
                    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));

            var currentDate = System.DateTime.Now;
            var zone = currentDate.ToString("yyyyMMddHHmmsszzz").Substring(14).Replace(":", string.Empty);

            // Basic valid tests
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyy"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMM"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMdd"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHH"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHHmm"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHHmmss"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHHmmss.f"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHHmmss.ff"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHHmmss.fff"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHHmmss.ffff"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHHmmss.fffff"));
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, currentDate.ToString("yyyyMMddHHmmss.ffffff"));

            // Basic valid UTC offset tests
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081204230259.165432+0000");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081204230259+0530");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "200812042302+0530");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "2008120423+0530");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081204-0100");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "200812+1400");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "2007-0500");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "2007+0500");

            // Random valid range tests
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "-2008");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "2008-");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "-20081204230259");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"-20081204230259.165432{zone}");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "2008-200912");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"200812-200812{zone}");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"20081204{zone}-20081204");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"2008120422{zone}-2008120423{zone}");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"2008120422-0200-2008120423");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "200812042202-200812042302");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"20081204220259.1{zone}-20081204230259.1");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081204220259.12-20081204230259.12");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081204220259.132-20081204230259.132");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081204220259-20081204230259.1432");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"20081204220259.15432{zone}-20081204230259.15432{zone}");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081204220259.165432-20081204230259.165432");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081204220259+0000-20081204230259.165432+0100");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"20081204220259.165432+0200-20081204230259.165432{zone}");
            ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"2019-1200-2020+1400");

            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "2019-1300-2020+1300"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "-0200"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20191000"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20190013"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "201912-0000"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "201912+1500"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "- "));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "201"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "2019-2020-2021"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20191021120000.000000-1300-20191022120000.000000-1300"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20191031+"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20081304230259"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "-20191031+2400"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20191031121200+02"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "20193010-20193110"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, "2019-0100-2020-0100-202002-0100"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ScheduledProcedureStepStartDateTime, $"20081-200812{zone}"));
        }


        [Fact]
        public void DicomValidation_ValidateIS()
        {
            // Integer String
            var ds =
                new DicomDatasetMock(
                    new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage),
                    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));

            ds.AddOrUpdate(DicomTag.ReferencedFrameNumber, "");
            ds.AddOrUpdate(DicomTag.ReferencedFrameNumber, "10");
            ds.AddOrUpdate(DicomTag.ReferencedFrameNumber, "10 ");

            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ReferencedFrameNumber, "abc"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ReferencedFrameNumber, "1 0"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ReferencedFrameNumber, "1-0"));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ReferencedFrameNumber, "12300283828189237809128312838677812649724"));
        }


        [Fact]
        public void DicomValidation_ValidateLT()
        {
            // Long Text
            var ds =
                new DicomDatasetMock(
                    new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage),
                    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));

            ds.AddOrUpdate(DicomTag.PulserNotes, "");
            ds.AddOrUpdate(DicomTag.PulserNotes, "Note");

            int maxLength = 10240;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            string bigString = new string(Enumerable.Range(1, maxLength).Select(_ => chars[random.Next(chars.Length)]).ToArray());

            ds.AddOrUpdate(DicomTag.PulserNotes, bigString);

            // Max is 10240
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.PulserNotes, bigString + "A"));
        }

        [Fact]
        public void DicomValidation_ValidateST()
        {
            // Short Text
            var ds =
                new DicomDatasetMock(
                    new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage),
                    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));

            int maxLength = 1024;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            string bigString = new string(Enumerable.Range(1, maxLength).Select(_ => chars[random.Next(chars.Length)]).ToArray());

            ds.AddOrUpdate(DicomTag.InstitutionAddress, bigString);

            // Max is 1024
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.InstitutionAddress, bigString + "A"));
        }


        [Fact]
        public void DicomValidation_ValidateUI()
        {
            // UID
            var ds =
                new DicomDatasetMock(
                    new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage),
                    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));


            string goodUID = "25326.718.62637.281936.21836.1263.628.40919.74.213123.5.123123.5";

            // Test 64 byte uid
            ds.AddOrUpdate(DicomTag.SOPClassUID, goodUID);

            // Test with characters
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.SOPClassUID, Guid.NewGuid().ToString()));

            // Max is 64bytes
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.SOPClassUID, goodUID + "9"));

            // Allowed only numbers and .
            string uid = goodUID.Substring(1);
            uid += "A";
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.SOPClassUID, uid));
        }

        [Fact]
        public void DicomValidation_ValidateSH()
        {
            // Short String
            var ds =
                new DicomDatasetMock(
                    new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage),
                    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));

            int maxLength = 16;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            string shortString = new string(Enumerable.Range(1, maxLength).Select(_ => chars[random.Next(chars.Length)]).ToArray());

            // Test empty
            ds.AddOrUpdate(DicomTag.ImplementationVersionName, "");

            // Test 16 byte string
            ds.AddOrUpdate(DicomTag.ImplementationVersionName, shortString);

            shortString.Substring(1);
            // Test 15 byte string
            ds.AddOrUpdate(DicomTag.ImplementationVersionName, shortString);

            // Add ESC
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ImplementationVersionName, shortString + '\u001b'));

            // Too long string
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ImplementationVersionName, shortString + "AB"));

        }

        [Fact]
        public void DicomValidation_ValidateCS()
        {
            // Code String
            var ds =
                new DicomDatasetMock(
                    new DicomUniqueIdentifier(DicomTag.SOPClassUID, DicomUID.SecondaryCaptureImageStorage),
                    new DicomUniqueIdentifier(DicomTag.SOPInstanceUID, "1.2.3"));

            int maxLength = 16;
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            Random random = new Random();
            string shortString = new string(Enumerable.Range(1, maxLength).Select(_ => chars[random.Next(chars.Length)]).ToArray());

            // Test empty
            ds.AddOrUpdate(DicomTag.ConversionType, "");

            // Test 16 byte string
            ds.AddOrUpdate(DicomTag.ConversionType, shortString);

            shortString.Substring(1);
            // Test 15 byte string
            ds.AddOrUpdate(DicomTag.ConversionType, shortString);

            // Only uppercase character, digits, space and underscore are allowed
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ConversionType, shortString + 'b'));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ConversionType, shortString + '-'));
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ConversionType, shortString + '\\'));

            // Too long string
            Assert.Throws<DicomValidationException>(() => ds.AddOrUpdate(DicomTag.ConversionType, shortString + "AB"));

        }
        #endregion

    }
}
