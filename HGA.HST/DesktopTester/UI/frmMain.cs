﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.IO;
using System.Reflection;
using System.Globalization;
using XyratexOSC.Logging;
using XyratexOSC.UI;
using Seagate.AAS.Utils;
using System.IO.Ports;
using DesktopTester.Data;
using DesktopTester.Data.IncomingTestProbeData;
using DesktopTester.Data.OutgoingTestProbeData;
using DesktopTester.Utils;
using DesktopTester.Settings;
using XyratexOSC.Settings;
using XyratexOSC.Settings.UI;

namespace DesktopTester.UI
{
    public partial class frmMain : Form
    {
        private frmFunctionalTestsRecipeCOMPortSettingsEditor _editRecipeForm;
        PropertyGridEditor _propertyGridCOMPortSettingsEditor;
        SettingsEditor COMPortSettingsEditor;
        bool DataReceived = false;
        bool CableCalibrationDataReceived = false;        
        bool HGAsLoaded = false;
        bool HGAsEjected = false;
        
        public frmMain()
        {
            InitializeComponent();
            headerUserAccess1.AssignSettings(ApplicationSettings.Instance, this);              
        }

        private void OnSettingsSave(object sender, EventArgs e)
        {
            ApplicationSettings.Instance.Save();
        }

        private void OnSettingsLoad(object sender, EventArgs e)
        {
            ApplicationSettings.Instance.Load();            
        }
        
        public HeaderUserAccess getHeaderUserAccess()
        {
            return headerUserAccess1;
        }

        public void SaveSettingsToFile(object sender, EventArgs e)
        {
            testProbeComPort.Close();
            ApplicationSettings.Instance.Save();
            OpenTestProbeCOMPort();
        }

        public void UpdateUsersSettings(object sender, EventArgs e)
        {
            try
            {
                UserAccessSettings UserAccessSettings = sender as UserAccessSettings;

                if (UserAccessSettings != null)
                {
                    ApplicationSettings.Instance.LoadUsers();
                }
            }
            catch
            {}
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            Log.Info("Startup", "HST Desktop Tester started.");
            cboDebugADCVoltageCh.SelectedIndex = 0;
            cboDebugBiasSensingGetBiasVoltagesHGA.SelectedIndex = 0;
            cboDebugMUXCh.SelectedIndex = 0;
            cboDebugResistanceCapacitanceHGA.SelectedIndex = 0;
            cboCableCalibrationChannelNumber.SelectedIndex = 0;
            cboCableCalibrationHGAIndex.SelectedIndex = 0;
            cboBenchTestsUpDownTabSelection.SelectedIndex = 0;
            cboCableCalibrationResistanceOrCapacitanceSelection.SelectedIndex = 0;
            cboCableCalibrationUpDownTabSelection.SelectedIndex = 0;
            cboPrecisorCompensationEnableFlag.SelectedIndex = 0;
            cboPrecisorCompensationUpDownTabSelection.SelectedIndex = 0;

            _editRecipeForm = new frmFunctionalTestsRecipeCOMPortSettingsEditor();
            lblCommonProductID.Text = CommonFunctions.Instance.strProductID;
                        
            settingsPanelUsersConfig.AssignObject(ApplicationSettings.Instance.getUserAccessSettings(), ApplicationSettings.Instance.UsersSettingsFilePath);
            settingsPanelUsersConfig.OnLoad += new EventHandler(OnSettingsLoad);
            settingsPanelUsersConfig.OnSave += new EventHandler(OnSettingsSave);

            COMPortSettingsEditor = new PropertyGridEditor(ApplicationSettings.Instance);
            _propertyGridCOMPortSettingsEditor = (PropertyGridEditor)COMPortSettingsEditor;
            COMPortSettingsEditor.FilePath = CommonFunctions.Instance.COMPortSettingsFileDirectory;
            COMPortSettingsEditor.Dock = DockStyle.Fill;
            TabPage COMPortTabPage = tabControlApplicationSettings.TabPages[0] as TabPage;
            COMPortTabPage.Controls.Add(COMPortSettingsEditor);
            COMPortSettingsEditor.SaveSettingsToXMLFile += SaveSettingsToFile;

            settingsPanelUsersConfig.UpdateUsersSettings += UpdateUsersSettings;

            OpenTestProbeCOMPort();  
        }

        private void frmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            SaveConfigurationToFile();
            testProbeComPort.Close();
            Log.Info("Application Terminating", "COM port: {0} has been closed.", testProbeComPort.PortName);  
        }                 
        
        public void OpenTestProbeCOMPort()
        {
            testProbeComPort.PortName = ApplicationSettings.Instance.TestProbe.COMPort.ToString();
            testProbeComPort.BaudRate = ApplicationSettings.Instance.TestProbe.BaudRate;
            testProbeComPort.DataBits = ApplicationSettings.Instance.TestProbe.DataBits;
            testProbeComPort.StopBits = ApplicationSettings.Instance.TestProbe.StopBits;
            testProbeComPort.Parity = ApplicationSettings.Instance.TestProbe.Parity;
            testProbeComPort.RtsEnable = false;
            testProbeComPort.DtrEnable = false;
            lblCommonCOMPort.Text = testProbeComPort.PortName;

            try
            {
                if(testProbeComPort.IsOpen == true)
                {
                    testProbeComPort.Close();
                    Log.Info("COM Port", "Previously opened COM port has been closed before opening a new session.");
                }
                testProbeComPort.Open();
                Log.Info("COM Port", "COM port: {0}, baud rate: {1}, data bits: {2}, stop bits: {3}, parity: {4} has been opened.",
                         testProbeComPort.PortName, testProbeComPort.BaudRate, testProbeComPort.DataBits, testProbeComPort.StopBits, testProbeComPort.Parity);
            }

            catch (Exception ex)
            {
                Notify.PopUpError("Serial Port Configuration Error", "Error opening serial port: " + ex.Message);
                Log.Error("COM Port", "Error opening serial port: {0}.", ex.Message);
            }                    
        }
    
        private void disableAllAPIButtons()
        {                        
            UIUtility.BeginInvoke(this, () =>
            {
                Cursor.Current = Cursors.WaitCursor;
                
                btnBenchTestsContinuousMeasure.Enabled = false;
                btnBenchTestsMultipleMeasure.Enabled = false;                
                btnBenchTestsSingleMeasure.Enabled = false;                
                btnBenchTestsStopMeasure.Enabled = false;
                btnBenchTestsClearDisplay.Enabled = false;

                btnFunctionalTestsStartSelfTests.Enabled = false;
                btnFunctionalTestsSaveTestRecord.Enabled = false;
                btnFunctionalTestsClearDisplay.Enabled = false;

                btnConfigurationSaveConfiguration.Enabled = false;

                btnPCBACalibrationStartAutoCalibration.Enabled = false;
                btnPCBACalibrationGetCalibrationData.Enabled = false;
                btnPCBACalibrationClearDisplay.Enabled = false;
                btnPCBACalibrationSaveCalibrationData.Enabled = false;

                btnPCBACalibrationManualCalibration0Ohm.Enabled = false;
                btnPCBACalibrationManualCalibration10Ohm.Enabled = false;
                btnPCBACalibrationManualCalibration100Ohm.Enabled = false;
                btnPCBACalibrationManualCalibration500Ohm.Enabled = false;
                btnPCBACalibrationManualCalibration1000Ohm.Enabled = false;
                btnPCBACalibrationManualCalibration10000Ohm.Enabled = false;
                btnPCBACalibrationManualCalibration100pF.Enabled = false;
                btnPCBACalibrationManualCalibration270pF.Enabled = false;
                btnPCBACalibrationManualCalibration470pF.Enabled = false;
                btnPCBACalibrationManualCalibration680pF.Enabled = false;
                btnPCBACalibrationManualCalibration820pF.Enabled = false;
                btnPCBACalibrationManualCalibration10nF.Enabled = false;
                btnPCBACalibrationTemperatureCalibration5Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration10Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration15Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration20Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration25Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration30Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration35Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration40Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration45Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration50Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration55Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration60Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration65Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration70Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration75Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration80Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration85Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration90Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration95Celcius.Enabled = false;
                btnPCBACalibrationTemperatureCalibration100Celcius.Enabled = false;
                btnPCBACalibrationManualCalibrationCalibrateOffset.Enabled = false;
                btnPCBACalibrationManualCalibrationGetCalibrationOffset.Enabled = false;

                btnCableCalibrationGetCableCalibrationData.Enabled = false;
                btnCableCalibrationSaveCalibrationData.Enabled = false;
                btnCableCalibrationStartCableCalibration.Enabled = false;
                btnClearCableCalibrationResults.Enabled = false;
                btnCableCalibrationSetCableCompensationResistanceCapacitance.Enabled = false;

                btnPrecisorCompensationGetPrecisorCapacitanceCompensation.Enabled = false;
                btnPrecisorCompensationSavePrecisorCapacitanceCompensation.Enabled = false;
                btnPrecisorCompensationSetPrecisorCapacitanceCompensation.Enabled = false;      

                btnDebugEEPROMWrite.Enabled = false;
                btnDebugEEPROMRead.Enabled = false;
                btnDebugDACWrite.Enabled = false;
                btnDebugDACRead.Enabled = false;
                btnDebugADCWrite.Enabled = false;
                btnDebugADCRead.Enabled = false;
                btnDebugMUXSetMUXSwitch.Enabled = false;
                btnDebugMUXGetCapacitanceReading.Enabled = false;
                btnDebugMUXGetProcessorStatus.Enabled = false;
                btnDebugTemperatureGetTemperatures.Enabled = false;
                btnDebugTemperatureStop.Enabled = false;
                btnDebugADCVoltageGetADCVoltages.Enabled = false;
                btnDebugADCVoltageStop.Enabled = false;
                btnDebugBiasSensingGetBiasVoltages.Enabled = false;
                btnDebugBiasSensingGetSensingVoltages.Enabled = false;
                btnDebugResistanceCapacitanceGetResults.Enabled = false;
                
                btnServoCalibrationAllMoveToStandbyPosition.Enabled = false;
                btnServoCalibrationDesktopTesterConnect.Enabled = false;
                btnServoCalibrationDesktopTesterDisconnect.Enabled = false;
                btnServoCalibrationDownTabOff.Enabled = false;
                btnServoCalibrationDownTabOn.Enabled = false;
                btnServoCalibrationFlattenerExtend1.Enabled = false;
                btnServoCalibrationFlattenerRetract1.Enabled = false;
                btnServoCalibrationFlattenerExtend2.Enabled = false;
                btnServoCalibrationFlattenerRetract2.Enabled = false;
                btnServoCalibrationGetAllCurrentPositions.Enabled = false;
                btnServoCalibrationGetAllInputs.Enabled = false;
                btnServoCalibrationHomeAll.Enabled = false;
                btnServoCalibrationHomePrecisor.Enabled = false;
                btnServoCalibrationHomeTestProbe.Enabled = false;
                btnServoCalibrationJogCCW.Enabled = false;
                btnServoCalibrationJogCW.Enabled = false;
                btnServoCalibrationJogDown.Enabled = false;
                btnServoCalibrationJogToInside.Enabled = false;
                btnServoCalibrationJogToLeft.Enabled = false;
                btnServoCalibrationJogToOutside.Enabled = false;
                btnServoCalibrationJogToRight.Enabled = false;
                btnServoCalibrationJogUp.Enabled = false;
                btnServoCalibrationLoadHGAs.Enabled = false;
                btnServoCalibrationMovePrecisorToFlatDownTabPosition.Enabled = false;
                btnServoCalibrationMovePrecisorToFlatUpTabPosition.Enabled = false;
                btnServoCalibrationMovePrecisorToLoadUnloadPosition.Enabled = false;
                btnServoCalibrationMovePrecisorToStandbyPosition.Enabled = false;
                btnServoCalibrationMovePrecisorToTargetPosition.Enabled = false;
                btnServoCalibrationMovePrecisorToTestDownTabPosition.Enabled = false;
                btnServoCalibrationMovePrecisorToTestUpTabPosition.Enabled = false;
                btnServoCalibrationMoveProbeToDownTabPosition.Enabled = false;
                btnServoCalibrationMoveProbeToStandbyPosition.Enabled = false;
                btnServoCalibrationMoveProbeToTargetPosition.Enabled = false;
                btnServoCalibrationMoveProbeToUpTabPosition.Enabled = false;
                btnServoCalibrationEjectHGAs.Enabled = false;
                btnServoCalibrationSaveAndDownloadCalibrationData.Enabled = false;
                btnServoCalibrationServoOff.Enabled = false;
                btnServoCalibrationTeachPrecisorFlatDownTabPosition.Enabled = false;
                btnServoCalibrationTeachPrecisorFlatUpTabPosition.Enabled = false;
                btnServoCalibrationTeachPrecisorLoadUnloadPosition.Enabled = false;
                btnServoCalibrationTeachPrecisorStandbyPosition.Enabled = false;
                btnServoCalibrationTeachPrecisorTestDownTabPosition.Enabled = false;
                btnServoCalibrationTeachPrecisorTestUpTabPosition.Enabled = false;
                btnServoCalibrationTeachProbeDownTabPosition.Enabled = false;
                btnServoCalibrationTeachProbeStandbyPosition.Enabled = false;
                btnServoCalibrationTeachProbeUpTabPosition.Enabled = false;
                btnServoCalibrationUpTabOff.Enabled = false;
                btnServoCalibrationUpTabOn.Enabled = false;

                btnDesktopTestsClearDisplay.Enabled = false;
                btnDesktopTestsConnectDesktopTester.Enabled = false;
                btnDesktopTestsContinuousMeasure.Enabled = false;
                btnDesktopTestsDisconnectDesktopTester.Enabled = false;
                btnDesktopTestsMultipleMeasure.Enabled = false;
                btnDesktopTestsSingleMeasure.Enabled = false;
                btnDesktopTestsStopMeasure.Enabled = false;                
            });   
        }

        private void enableAllAPIButtons()
        {
            UIUtility.BeginInvoke(this, () =>
            {
                Cursor.Current = Cursors.Default;
                btnBenchTestsContinuousMeasure.Enabled = true;
                btnBenchTestsMultipleMeasure.Enabled = true;                
                btnBenchTestsSingleMeasure.Enabled = true;                
                btnBenchTestsStopMeasure.Enabled = true;
                btnBenchTestsClearDisplay.Enabled = true;

                btnFunctionalTestsStartSelfTests.Enabled = true;
                btnFunctionalTestsSaveTestRecord.Enabled = true;
                btnFunctionalTestsClearDisplay.Enabled = true;

                btnConfigurationSaveConfiguration.Enabled = true;

                btnPCBACalibrationStartAutoCalibration.Enabled = true;
                btnPCBACalibrationGetCalibrationData.Enabled = true;
                btnPCBACalibrationClearDisplay.Enabled = true;
                btnPCBACalibrationSaveCalibrationData.Enabled = true;

                btnPCBACalibrationManualCalibration0Ohm.Enabled = true;
                btnPCBACalibrationManualCalibration10Ohm.Enabled = true;
                btnPCBACalibrationManualCalibration100Ohm.Enabled = true;
                btnPCBACalibrationManualCalibration500Ohm.Enabled = true;
                btnPCBACalibrationManualCalibration1000Ohm.Enabled = true;
                btnPCBACalibrationManualCalibration10000Ohm.Enabled = true;
                btnPCBACalibrationManualCalibration100pF.Enabled = true;
                btnPCBACalibrationManualCalibration270pF.Enabled = true;
                btnPCBACalibrationManualCalibration470pF.Enabled = true;
                btnPCBACalibrationManualCalibration680pF.Enabled = true;
                btnPCBACalibrationManualCalibration820pF.Enabled = true;
                btnPCBACalibrationManualCalibration10nF.Enabled = true;
                btnPCBACalibrationTemperatureCalibration5Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration10Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration15Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration20Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration25Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration30Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration35Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration40Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration45Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration50Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration55Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration60Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration65Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration70Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration75Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration80Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration85Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration90Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration95Celcius.Enabled = true;
                btnPCBACalibrationTemperatureCalibration100Celcius.Enabled = true;
                btnPCBACalibrationManualCalibrationCalibrateOffset.Enabled = true;
                btnPCBACalibrationManualCalibrationGetCalibrationOffset.Enabled = true;

                btnCableCalibrationGetCableCalibrationData.Enabled = true;
                btnCableCalibrationSaveCalibrationData.Enabled = true;
                btnCableCalibrationStartCableCalibration.Enabled = true;
                btnClearCableCalibrationResults.Enabled = true;
                btnCableCalibrationSetCableCompensationResistanceCapacitance.Enabled = true;

                btnPrecisorCompensationGetPrecisorCapacitanceCompensation.Enabled = true;
                btnPrecisorCompensationSavePrecisorCapacitanceCompensation.Enabled = true;
                btnPrecisorCompensationSetPrecisorCapacitanceCompensation.Enabled = true;                

                btnDebugEEPROMWrite.Enabled = true;
                btnDebugEEPROMRead.Enabled = true;
                btnDebugDACWrite.Enabled = true;
                btnDebugDACRead.Enabled = true;
                btnDebugADCWrite.Enabled = true;
                btnDebugADCRead.Enabled = true;
                btnDebugMUXSetMUXSwitch.Enabled = true;
                btnDebugMUXGetCapacitanceReading.Enabled = true;
                btnDebugMUXGetProcessorStatus.Enabled = true;
                btnDebugTemperatureGetTemperatures.Enabled = true;
                btnDebugTemperatureStop.Enabled = true;
                btnDebugADCVoltageGetADCVoltages.Enabled = true;
                btnDebugADCVoltageStop.Enabled = true;
                btnDebugBiasSensingGetBiasVoltages.Enabled = true;
                btnDebugBiasSensingGetSensingVoltages.Enabled = true;
                btnDebugResistanceCapacitanceGetResults.Enabled = true;

                btnServoCalibrationAllMoveToStandbyPosition.Enabled = true;
                btnServoCalibrationDesktopTesterConnect.Enabled = true;
                btnServoCalibrationDesktopTesterDisconnect.Enabled = true;
                btnServoCalibrationDownTabOff.Enabled = true;
                btnServoCalibrationDownTabOn.Enabled = true;
                btnServoCalibrationFlattenerExtend1.Enabled = true;
                btnServoCalibrationFlattenerRetract1.Enabled = true;
                btnServoCalibrationFlattenerExtend2.Enabled = true;
                btnServoCalibrationFlattenerRetract2.Enabled = true;
                btnServoCalibrationGetAllCurrentPositions.Enabled = true;
                btnServoCalibrationGetAllInputs.Enabled = true;
                btnServoCalibrationHomeAll.Enabled = true;
                btnServoCalibrationHomePrecisor.Enabled = true;
                btnServoCalibrationHomeTestProbe.Enabled = true;
                btnServoCalibrationJogCCW.Enabled = true;
                btnServoCalibrationJogCW.Enabled = true;
                btnServoCalibrationJogDown.Enabled = true;
                btnServoCalibrationJogToInside.Enabled = true;
                btnServoCalibrationJogToLeft.Enabled = true;
                btnServoCalibrationJogToOutside.Enabled = true;
                btnServoCalibrationJogToRight.Enabled = true;
                btnServoCalibrationJogUp.Enabled = true;
                btnServoCalibrationLoadHGAs.Enabled = true;
                btnServoCalibrationMovePrecisorToFlatDownTabPosition.Enabled = true;
                btnServoCalibrationMovePrecisorToFlatUpTabPosition.Enabled = true;
                btnServoCalibrationMovePrecisorToLoadUnloadPosition.Enabled = true;
                btnServoCalibrationMovePrecisorToStandbyPosition.Enabled = true;
                btnServoCalibrationMovePrecisorToTargetPosition.Enabled = true;
                btnServoCalibrationMovePrecisorToTestDownTabPosition.Enabled = true;
                btnServoCalibrationMovePrecisorToTestUpTabPosition.Enabled = true;
                btnServoCalibrationMoveProbeToDownTabPosition.Enabled = true;
                btnServoCalibrationMoveProbeToStandbyPosition.Enabled = true;
                btnServoCalibrationMoveProbeToTargetPosition.Enabled = true;
                btnServoCalibrationMoveProbeToUpTabPosition.Enabled = true;
                btnServoCalibrationEjectHGAs.Enabled = true;
                btnServoCalibrationSaveAndDownloadCalibrationData.Enabled = true;
                btnServoCalibrationServoOff.Enabled = true;
                btnServoCalibrationTeachPrecisorFlatDownTabPosition.Enabled = true;
                btnServoCalibrationTeachPrecisorFlatUpTabPosition.Enabled = true;
                btnServoCalibrationTeachPrecisorLoadUnloadPosition.Enabled = true;
                btnServoCalibrationTeachPrecisorStandbyPosition.Enabled = true;
                btnServoCalibrationTeachPrecisorTestDownTabPosition.Enabled = true;
                btnServoCalibrationTeachPrecisorTestUpTabPosition.Enabled = true;
                btnServoCalibrationTeachProbeDownTabPosition.Enabled = true;
                btnServoCalibrationTeachProbeStandbyPosition.Enabled = true;
                btnServoCalibrationTeachProbeUpTabPosition.Enabled = true;
                btnServoCalibrationUpTabOff.Enabled = true;
                btnServoCalibrationUpTabOn.Enabled = true;

                btnDesktopTestsClearDisplay.Enabled = true;
                btnDesktopTestsConnectDesktopTester.Enabled = true;
                btnDesktopTestsContinuousMeasure.Enabled = true;
                btnDesktopTestsDisconnectDesktopTester.Enabled = true;
                btnDesktopTestsMultipleMeasure.Enabled = true;
                btnDesktopTestsSingleMeasure.Enabled = true;
                btnDesktopTestsStopMeasure.Enabled = true;             
            });            
        }

        private void frmMain_Shown(object sender, EventArgs e)
        {            
            TestProbeAPICommand APICommand = new TestProbeAPICommand(TestProbeAPICommand.HST_get_product_id_Message_ID, TestProbeAPICommand.HST_get_product_id_Message_Size, null);
            CommonFunctions.Instance.OutgoingTestProbeDataAPIs.Enqueue(APICommand);
            APICommand = new TestProbeAPICommand(TestProbeAPICommand.HST_get_operation_mode_Message_ID, TestProbeAPICommand.HST_get_operation_mode_Message_Size, null);
            CommonFunctions.Instance.OutgoingTestProbeDataAPIs.Enqueue(APICommand);
            APICommand = new TestProbeAPICommand(TestProbeAPICommand.HST_get_firmware_version_Message_ID, TestProbeAPICommand.HST_get_firmware_version_Message_Size, null);
            CommonFunctions.Instance.OutgoingTestProbeDataAPIs.Enqueue(APICommand);

            bool commandSentToMicroprocessor = constructAndSendWriteDataBuffer();

            lblCommonSoftwareVersion.Text = "v" + Assembly.GetExecutingAssembly().GetName().Version.ToString();
            Thread.CurrentThread.CurrentCulture = new CultureInfo("fr-FR");
            DateTime today = DateTime.Now;
            lblCommonTestDate.Text = today.ToShortDateString();

            btnConfigurationLoadConfiguration_Click(sender, e);
            IsConfigurationSetupTempered = false;
            tabPageConfigurationSetup.Refresh();
        }

        private void tabDesktopTester_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (tabDesktopTester.SelectedIndex)
            {  
                case 0:  // Bench Tests                   
                    break;
                case 1:  // Functional Tests                           
                    CommonFunctions.Instance.LoadFunctionalTestsRecipe(); 
                    break;
                case 2:  // Configuration & Setup                    
                    break;
                case 3:  // PCBA Calibration                    
                    IsPCBACalibrationTempered = false;
                    tabPagePCBACalibration.Refresh();
                    break;
                case 4:  // Cable Calibration
                    IsCableCalibrationTempered = false;
                    tabPageCableCalibration.Refresh();
                    break;
                case 5:  // Precisor Compensation
                    IsPrecisorCompensationTempered = false;
                    tabPagePrecisorCompensation.Refresh();
                    break;
                case 6:  // Debug  
                case 7:  // Servo Calibration  
                case 8:  // Desktop Tests  
                case 9:  // Application Settings
                    break;
            }            
        }

        public TabControl getTabControl()
        {
            return tabDesktopTester;
        }        
    }
}
