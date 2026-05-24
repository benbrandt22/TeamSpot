import usb_hid

HID_REPORT_DESCRIPTOR = bytes((
    0x06, 0x22, 0xFF,    # UsagePage(LitButton[0xFF22])
    0x09, 0x01,          # UsageId(LitButton[0x0001])
    0xA1, 0x01,          # Collection(Application)
    0x85, 0x01,          #     ReportId(1)
    0x09, 0x02,          #     UsageId(Button[0x0002])
    0x15, 0x00,          #     LogicalMinimum(0)
    0x25, 0x01,          #     LogicalMaximum(1)
    0x95, 0x01,          #     ReportCount(1)
    0x75, 0x01,          #     ReportSize(1)
    0x81, 0x02,          #     Input(Data, Variable, Absolute, NoWrap, Linear, PreferredState, NoNullPosition, BitField)
    0x75, 0x07,          #     ReportSize(7)
    0x81, 0x03,          #     Input(Constant, Variable, Absolute, NoWrap, Linear, PreferredState, NoNullPosition, BitField)
    0x09, 0x03,          #     UsageId(RedChannel[0x0003])
    0x09, 0x04,          #     UsageId(GreenChannel[0x0004])
    0x09, 0x05,          #     UsageId(BlueChannel[0x0005])
    0x26, 0xFF, 0x00,    #     LogicalMaximum(255)
    0x95, 0x03,          #     ReportCount(3)
    0x75, 0x08,          #     ReportSize(8)
    0x91, 0x02,          #     Output(Data, Variable, Absolute, NoWrap, Linear, PreferredState, NoNullPosition, NonVolatile, BitField)
    0x09, 0x06,          #     UsageId(Brightness[0x0006])
    0x25, 0x64,          #     LogicalMaximum(100)
    0x95, 0x01,          #     ReportCount(1)
    0x75, 0x07,          #     ReportSize(7)
    0x91, 0x02,          #     Output(Data, Variable, Absolute, NoWrap, Linear, PreferredState, NoNullPosition, NonVolatile, BitField)
    0x75, 0x01,          #     ReportSize(1)
    0x91, 0x03,          #     Output(Constant, Variable, Absolute, NoWrap, Linear, PreferredState, NoNullPosition, NonVolatile, BitField)
    0xC0,                # EndCollection()
))

my_device = usb_hid.Device(
    report_descriptor=HID_REPORT_DESCRIPTOR,
    usage_page=0xFF22,
    usage=0x01,
    report_ids=(1,),
    in_report_lengths=(1,),
    out_report_lengths=(4,)
)

usb_hid.enable( (my_device,) )
