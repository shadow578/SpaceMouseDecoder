import serial
from time import sleep

class SpaceMouse:
    def __init__(self, port: str):
        self.__ser = serial.Serial(baudrate=9600,
                                 bytesize=8,
                                 parity="N",
                                 stopbits=1,
                                 rtscts=True, # required since RTS is used to power the hardware
                                 )
        self.__ser.port = port # so it's not opened immediately

        self.__rx_state = self.STATE_IDLE
        self.__rx_buffer = ""

        self.hardware_version = "unknown"
    
    def initialize(self):
        """intialize the hardware to the expected modes"""
        self.__ser.open()

        self.send(self.COMMAND_RESET)
        sleep(0.2)
        self.send(self.COMMAND_GET_VERSION)
        sleep(0.2)
        self.send(self.COMMAND_ENABLE_BUTTON_MESSAGES)
        sleep(0.2)
        self.send(self.COMMAND_SET_MODE_3)
        sleep(0.2)
        self.send(self.COMMAND_ZERO)

    def read_pending(self):
        """read all pending bytes from the hardware. non-blocking"""
        if not self.__ser.is_open:
            return

        if self.__ser.in_waiting > 0:
            chars = self.__ser.read(self.__ser.in_waiting).decode("ascii")
            self.push_chars(chars)


    #####################
    ## TX and Commands ##
    #####################

    COMMAND_RESET = "\rvt\r"
    COMMAND_GET_VERSION = "vQ\r"
    COMMAND_ENABLE_BUTTON_MESSAGES = "kQ\r"
    COMMAND_SET_MODE_3 = "m3\r"
    COMMAND_ZERO = "z\r"


    def send(self, command: str):
        """send a command to the hardware"""
        self.__ser.write(command.encode("ascii"))
        self.__ser.flush()


    ######################
    ## RX state machine ##
    ######################

    STATE_IDLE = "idle" # line idle, waiting for message start
    STATE_KEY_MESSAGE = "k_wait" # waiting to complete 'k' message
    STATE_POS_MESSAGE = "d_wait" # waiting to complete 'd' message
    STATE_VER_MESSAGE = "v_wait" # waiting to complete 'v' message
    

    def push_chars(self, chars: str):
        """push a string of characters received from the hardware into the state machine"""
        for c in chars:
            self.__push_char(c)


    def __push_char(self, c: str):
        """push a single character received from the hardware into the state machine"""
        assert len(c) == 1

        # start of new message?
        if self.__rx_state == self.STATE_IDLE:
            if c == "k":
                self.__rx_state = self.STATE_KEY_MESSAGE
                self.__rx_buffer = ""
            elif c == "d":
                self.__rx_state = self.STATE_POS_MESSAGE
                self.__rx_buffer = ""
            elif c == "v":
                self.__rx_state = self.STATE_VER_MESSAGE
                self.__rx_buffer = ""
            elif c == "b":
                # beep message
                print("beep!")
            elif c == "m":
                # mode change message
                print("mode change!")
            elif c == "z":
                # reset message
                print("reset!")
            elif c == "n":
                # zero radius
                print("zero radius!")
            elif c == "q":
                # set sentitivity message
                print("sensitivity set!")
            elif c == "e":
                # communication (?) error
                print("HW error!")
            elif c == "\r":
                # stray message end, ignore
                pass
            else:
                print(f"got unexpected char '{c}' in state {self.__rx_state}")
        
        # key message?
        elif self.__rx_state == self.STATE_KEY_MESSAGE:
            # append to buffer
            self.__rx_buffer += c

            # if we reached 3 payload bytes, process the message
            if len(self.__rx_buffer) == 3:
                self.__rx_state = self.STATE_IDLE
                self.__on_key_message(self.__rx_buffer)

        # position message?
        elif self.__rx_state == self.STATE_POS_MESSAGE:
            # append to buffer
            self.__rx_buffer += c

            # if we reached 24 payload bytes, process the message
            # this does NOT take into account messages without angles, 
            # so only modes when the mouse is in mode 3 
            if len(self.__rx_buffer) == 24:
                self.__rx_state = self.STATE_IDLE
                self.__on_pos_message(self.__rx_buffer)
        
        # version message?
        elif self.__rx_state == self.STATE_VER_MESSAGE:
            # payload ends at \r character
            if c == "\r":
                self.__rx_state = self.STATE_IDLE
                self.__on_version_message(self.__rx_buffer)
            else:
                self.__rx_buffer += c

        # unknown state?!
        else:
            print(f"got into unknown state {self.__rx_state}, resetting...")
            self.__rx_state = self.STATE_IDLE
    

    def __on_key_message(self, payload: str):
        """process a key message. payload is 3 chars"""
        assert len(payload) == 3
        print(f"raw key message: {payload}")

        k0 = self.__decode_nibble(payload[0])
        k1 = self.__decode_nibble(payload[1])
        k2 = self.__decode_nibble(payload[2])
        key = k0 + (k1 * 16) + (k2 * 256)

        print(f"Key States: {key:012b}")


    def __on_pos_message(self, payload: str):
        """process a position message. payload is 24 chars"""
        assert len(payload) == 24
        print(f"raw pos message: {payload}")

        x = self.__decode_signed_word(payload[0:4])
        y = self.__decode_signed_word(payload[8:12])
        z = self.__decode_signed_word(payload[4:8])
        u = self.__decode_signed_word(payload[20:24]) # theta X
        v = self.__decode_signed_word(payload[12:16]) # theta Y
        w = self.__decode_signed_word(payload[16:20]) # theta Z

        print(f"Position: x={x}, y={y}, z={z} | Angles: u={u}, v={v}, w={w}")


    def __on_version_message(self, payload: str):
        """process a version message. payload is variable length string"""
        payload = payload.strip()
        print(f"Got Version: {payload}")
        self.hardware_version = payload

    def __decode_signed_word(self, chars: str):
        """decode a 4-character word into a signed number (as used in position and angle)"""
        assert len(chars) == 4
        n0 = self.__decode_nibble(chars[0])
        n1 = self.__decode_nibble(chars[1])
        n2 = self.__decode_nibble(chars[2])
        n3 = self.__decode_nibble(chars[3])

        sign = (n0 & 0x8) == 0x8 # sign bit is the MSB of the first nibble
        n0 &= 0x7 # clear sign bit

        word = n0 + (n1 * 16) + (n2 * 256) + (n3 * 4096)

        # apply sign
        if sign:
            word = -word
        return word


    def __decode_nibble(self, char: str):
        """decode a character into a nibble"""
        assert len(char) == 1

        if char == "0": return 0
        elif char == "A": return 1
        elif char == "B": return 2
        elif char == "3": return 3
        elif char == "D": return 4
        elif char == "5": return 5
        elif char == "6": return 6
        elif char == "G": return 7
        elif char == "H": return 8
        elif char == "9": return 9
        elif char == ":": return 10
        elif char == "K": return 11
        elif char == "<": return 12
        elif char == "M": return 13
        elif char == "N": return 14
        elif char == "?": return 15
        else: return 0


if __name__ == "__main__":
    sm = SpaceMouse("COM4")
    sm.initialize()

    sm.send("b\r") # beep after init
       
    while True:
        sm.read_pending()
        sleep(0.1) # don't hog the CPU