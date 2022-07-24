# IgnitionHelper
An app that helps Ignition Visualization Designer with some of the tasks.

Features:
1. Duplicates Instances od Data Types (Exported from Ignition (.xml)) according to Exported Tags (from Studio5000, (.xlsx))
  Note: Data Type name of PLC Data Block needs to consist name of Data Type in Ignition Designer.
  Manual to use feature:
  1.1. Export All PLC tags from Studio 5000 (.csv)
  1.2. Convert .csv file to .xlsx file
  1.3. Export All PLC tags from Ignition Designer (.xml)
  1.4. Click button "Select Exp Tags (.XLSX)" and chose file created in point (1.2)
  1.5. Click button "Generate XML" and chose file created in point (1.3)
  1.6. Import (option "Direct") generated (*_edit.xml) file to Studio 5000 (PLC Tags branch)
