using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

/*
 * 13/03/2013 - 1.0.0.0 - first version.
 * 04/04/2013 - 1.0.0.1 - actually use the check sum .
 * 22/05/2013 - 1.0.0.2 - fit software to work with few sensors in same box.
 *                        add option of change id from monitor
 * 22/05/2013 - 1.0.0.3 - add reset button. when press on it all data reset. sensor can reconnect  to monitor.
 * 28/05/2013 -           add few try-catch to code to protect from crashing
 * 17/06/2013 - 1.0.0.4 -   1. use the check sum 
 *                          2. disable do SET to rom version  
 *                          3. fix bug in func StrtoBytes: if length of string more than bytes to copy
 *                          4. fix bug of send negative GMT
 *                          5. fix bug of set password - set buffer as 6 bytes not 32
 * 30/07/2013             HelloSensor: set selected index only if managed to get ID.
 * 05/08/2013             1. add monitor option: allow user to watch connection through modem.
 *                        2. change title of Roamin to use country code and swap the meaning of yes/no
 * 12/09/2013 - 1.0.0.5   updating first sensor's ID cause all next sensors get consecutive ID numbers 
 * 30/09/2013   1.0.0.6   redesign of window. seperate parameters to 3 main groups: sensor params, loggers general params, logger connection params.
 *                        remove use in GMT.
 * 10/2013      1.0.0.7   add option to define the inputs of the logger (which sensor type connected to which IO).
 *                          all possible types being read from DB of amazon2 server. user can define inputs.
 *10/11/2013    1.0.0.8     in order to use RTS19 and RTS20 with the same program - check rom version right after connecting, and if its rom for 
 *                          RTS19 - disable set/use 4th input. else - enable it.
 *19/11/2013    1.0.0.9 -   add try & catch to get table of types from sql
 *23/12/2013    1.0.0.10    Input definition - add ability to define RTS21 sensors (7 inputs all in all) 
 *05/01/2014                fix bug with hive scale special parameters. (remove group box)
 *28/01/2014    1.0.1.0     parameters of hive scale - redraw groupbox. user can sign it manually. both checkboxs not checked by "Select All" btn
 *                          for rain meter - optinal to define rain or water meter.
 *24/02/2014    1.0.1.1     add option of send measure now command to sensor and start measuring proccess without disconnecting monitor.
 *                          the "Measure Now" btn on "Sensor Parameters" tab send "get" command and gets back number of seconds to wait till 
 *                          measuring will end. add AutoMsg form to dispaly waiting msg while sensor is measuring.
 *03/03/2014    1.0.1.2     1. option to save and load all logger parameters in/from xml file
 *                          2. option to get logger parameters (connection parameters, sensors...) from sql DB.
 *30/03/2014    1.0.1.3     fix bug when getting logger parameters from sql DB: check if there are no defined sensor for logger before trying to show them.
 *01/04/2014                add m_RomVer member to save rom version of connected sensor.
 *                          1. do not show combination tab for RTS19 sensors. (tabControl1_Selecting)
 *                          2. disable "Measure Now" btn for sensors from un-fit versions.
 *02/04/2014                fine tuning of save & load from XML file. 
 *20/10/2014    1.0.1.4     set to combo box roaming "No" as default value.
 *02/11/2014    1.0.1.5     1. add try-catch to SaveSensorXML func.
 *                          2. adjust software to connect with satellite modem logger.
 *11/12/2014    1.0.1.6     1. more adjustments to satellite modem
 *                          2. add Water Flow sensor option on IO 5th
 *21/04/2015    1.0.1.7     Add "ID Generator" btn to get next available ID from web   
 *20/10/2015    1.0.2.0     make few modifications to work with logger of wireless sensors. exe called: Monitor-n-Config-WL.
 *                          mainly - disable input definition. disable change ID for all sensors, but only to logger. distinguish between 
 *16/11/2015    1.0.2.10    Setup the Order Information control. send all relevant data to ZOHO.
 *24/11/2015                change for ID allocation - send another parameter to api- if wireless -1 if not - 0.
 *04/01/2016    1.0.2.11    1. insert read of "Hardwares" table with all kinds of haardware versions, in ordder to update the combo
 *                          in Product Parameters.
 *                          2. instead of read sensors_type data table from amazon server - read it from main server.
 *27/03/2016    1.0.2.12    move "intervals" parameter to "sensor parameter" tab. if no sensors in logger - do not get it.
 *26/04/2017    1.0.2.20   FIRST Monitor for Wireless Logger.
 *09/05/2017    1.0.2.21    change COM baudrate to 38400.
 *                          add "Burm software" tab. user select 3 files to burn and than burn them to logger & receiver.
 *10/05/2017    1.0.2.23    1. end burn issue.
 *                          2. print text nicelly when its not connected to logger.
 *16/05/2017    1.0.2.24    change directory of phytech burn
 *21/05/2017    1.0.2.26    when generates ID - automatically send it to the logger.
							when burn logger - change burn low fuse from 0xDF to 0xEF
 *21/05/2017    1.0.2.26    when start burn - clear log. change colors of "burn" buttons - first normal, on proccess - orange
 *                          on success - green, else red.
 *23/07/2017    1.0.2.27    add to stickers the australian rcm (Regulatory Compliance Mark)
 *26/11/2017    1.0.2.28    change in func generate ID - different answer. parseit differently.
 *03/12/2017    1.0.2.29    1. SendLoggerInfo - send logger parameters to server instead via zoho.
 *                          2. re-set phytech icon to app in this development environment
 *21/12/2017    1.0.2.30    burn logger programs via batch file "burn_logger.bat". use function BurnLogger1.                           
 *24/12/2017    1.0.2.31    burn logger using different program - "atprogram.exe" - part of atmel studio.
 *                          get the program path from registry.
 *28/01/2018    1.0.2.32    replace barcode with QR code. print the right sticker twice.
*/

namespace XP_Monitor
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
