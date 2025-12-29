# -*- coding: UTF-8 -*-
import serial  # 引入串口库（需要额外安装）
import serial.tools.list_ports
import time  # 引入延时库
import threading  # 引入定时回调库
import psutil  # 引入psutil获取设备信息（需要额外安装）
import os  # 用于读取文件
import pyautogui  # 用于截图(需要额外安装pillow)
from datetime import datetime, timedelta  # 用于获取当前时间
import tkinter as tk
from tkinter import *  # 引入UI库
import tkinter.filedialog  # 用于获取文件路径
from PIL import Image, ImageDraw, ImageFont  # 引入PIL库进行图像处理
import sys  # 用于关闭程序
import autoit,re

gpu_num=input("独立显卡输入1,集成显卡输入0：")

MG_daemon_should_stop = False


# import numpy as np#使用numpy加速数据处理

class MSN_Device:  # 定义一个结构体
    def __init__(self, com, version):
        self.com = com  # 登记串口位置
        self.version = version  # 登记MSN版本
        self.name = 'MSN'  # 登记设备名称
        self.baud_rate = 19200  # 登记波特率


My_MSN_Device = []  # 创建一个空的结构体数组


class MSN_Data:  # 定义一个结构体
    def __init__(self, name, unit, family, data):
        self.name = name
        self.unit = unit
        self.family = family
        self.data = data


My_MSN_Data = []  # 创建一个空的结构体数组

# 颜色对应的RGB565编码
RED = 0xf800
GREEN = 0x07e0
BLUE = 0x001f
WHITE = 0xffff
BLACK = 0x0000
YELLOW = 0xFFE0
GRAY0 = 0xEF7D
GRAY1 = 0x8410
GRAY2 = 0x4208

hex_code = b''

G_screnn0 = bytearray()  # 空数组
G_screnn1 = bytearray()  # 空数组
Img_data_use = bytearray()  # 空数组
G_screnn0_OK = 0
G_screnn1_OK = 0
size_USE_X1 = 0
size_USE_Y1 = 0

# 参数定义
Show_W = 500  # 显示宽度
Show_H = 350  # 画布高度

# 按键功能定义
def Get_Photo_Path1():  # 获取文件路径
    global photo_path1, Label3
    photo_path1 = tk.filedialog.askopenfilename(title="选择文件",
                                                filetypes=[('Image file', '*.jpg'), ('Image file', '*.jpeg'),
                                                           ('Image file', '*.png'), ('Image file', '*.bmp')])
    Label3.config(text=photo_path1[-20:])

    # photo_path1=photo_path1[:-4]
    # print(photo_path1)


def Get_Photo_Path2():  # 获取文件路径
    global photo_path2, Label4
    photo_path2 = tk.filedialog.askopenfilename(title="选择文件", filetypes=[('Bin file', '*.bin')])
    Label4.config(text=photo_path2[-20:])
    photo_path2 = photo_path2[:-4]
    # print(photo_path2)


def Get_Photo_Path3():  # 获取文件路径
    global photo_path3, Label5  # 支持JPG、PNG、BMP图像格式
    photo_path3 = tk.filedialog.askopenfilename(title="选择文件",
                                                filetypes=[('Image file', '*.jpg'), ('Image file', '*.jpeg'),
                                                           ('Image file', '*.png'), ('Image file', '*.bmp')])
    Label5.config(text=photo_path3[-20:])

    # photo_path3=photo_path3[:-4]
    # print(photo_path3)


def Get_Photo_Path4():  # 获取文件路径
    global photo_path4, Label6
    photo_path4 = tk.filedialog.askopenfilename(title="选择文件",
                                                filetypes=[('Image file', '*.jpg'), ('Image file', '*.jpeg'),
                                                           ('Image file', '*.png'), ('Image file', '*.bmp')])
    Label6.config(text=photo_path4[-20:])

    # photo_path4=photo_path4[:-4]
    # print(photo_path4)


def Writet_Photo_Path1():  # 写入文件
    global photo_path1, write_path1, Text1, Img_data_use
    if write_path1 == 0:  # 确保上次执行写入完毕
        Text1.delete(1.0, END)  # 清除文本框
        Text1.insert(END, '图像格式转换...\n')  # 在文本框开始位置插入“内容一”
        im1 = Image.open(photo_path1)
        if im1.width >= (im1.height * 2):  # 图片长宽比例超过2:1
            im2 = im1.resize((int(80 * im1.width / im1.height), 80))
            Img_m = int(im2.width / 2)
            box = ((Img_m - 80, 0, Img_m + 80, 80))  # 定义需要裁剪的空间
            im2 = im2.crop(box)
        else:
            im2 = im1.resize((160, int(160 * im1.height / im1.width)))
            Img_m = int(im2.height / 2)
            box = ((0, Img_m - 40, 160, Img_m + 40))  # 定义需要裁剪的空间
            im2 = im2.crop(box)
        im2 = im2.convert('RGB')  # 转换为RGB格式
        Img_data_use = bytearray()  # 空数组
        for y in range(0, 80):  # 逐字解析编码
            for x in range(0, 160):  # 逐字解析编码
                r, g, b = im2.getpixel((x, y))
                Img_data_use.append(((r >> 3) << 3) | (g >> 5))
                Img_data_use.append((((g % 32) >> 2) << 5) | (b >> 3))
        write_path1 = 1


def Writet_Photo_Path2():  # 写入文件
    global photo_path2, write_path2, Text1
    if write_path2 == 0:  # 确保上次执行写入完毕
        write_path2 = 1
        Text1.delete(1.0, END)  # 清除文本框
        Text1.insert(END, '准备烧写Flash固件...\n')  # 在文本框开始位置插入“内容一”


def Writet_Photo_Path3():  # 写入文件
    global photo_path3, write_path3, Text1, Img_data_use
    if write_path3 == 0:  # 确保上次执行写入完毕
        Text1.delete(1.0, END)  # 清除文本框
        Text1.insert(END, '图像格式转换...\n')  # 在文本框开始位置插入“内容一”

        im1 = Image.open(photo_path3)
        if im1.width >= (im1.height * 2):  # 图片长宽比例超过2:1
            im2 = im1.resize((int(80 * im1.width / im1.height), 80))
            Img_m = int(im2.width / 2)
            box = ((Img_m - 80, 0, Img_m + 80, 80))  # 定义需要裁剪的空间
            im2 = im2.crop(box)
        else:
            im2 = im1.resize((160, int(160 * im1.height / im1.width)))
            Img_m = int(im2.height / 2)
            box = ((0, Img_m - 40, 160, Img_m + 40))  # 定义需要裁剪的空间
            im2 = im2.crop(box)
        im2 = im2.convert('RGB')  # 转换为RGB格式
        Img_data_use = bytearray()  # 空数组
        for y in range(0, 80):  # 逐字解析编码
            for x in range(0, 160):  # 逐字解析编码
                r, g, b = im2.getpixel((x, y))
                Img_data_use.append(((r >> 3) << 3) | (g >> 5))
                Img_data_use.append((((g % 32) >> 2) << 5) | (b >> 3))
        write_path3 = 1

        # print(img_use)

        # im2.show()
        # Text1.insert(END,'准备烧写背景图像...\n')#在文本框开始位置插入“内容一”


def Writet_Photo_Path4():  # 写入文件
    global photo_path4, write_path4, Text1, Img_data_use
    if write_path4 == 0:  # 确保上次执行写入完毕
        Text1.delete(1.0, END)  # 清除文本框
        Text1.insert(END, '动图格式转换中...\n')  # 在文本框开始位置插入“内容一”
        time.sleep(0.1)
        Path_use = photo_path4
        if Path_use[-4] == '.':  #
            write_path4 = Path_use[-4:]
            Path_use = Path_use[:-5]

        elif Path_use[-5] == '.':
            write_path4 = Path_use[-5:]
            Path_use = Path_use[:-6]
        else:
            Text1.insert(END, '动图名称不符合要求！\n')  # 在文本框开始位置插入“内容一”
            return  # 如果文件名不符合要求，直接返回

        Img_data_use = bytearray()
        u_time = time.time()
        for i in range(0, 36):  # 依次转换36张图片
            file_path = Path_use + str(i) + write_path4
            if not os.path.exists(file_path):  # 检查文件是否存在
                Text1.insert(END, '缺少动图文件：' + file_path + '\n')
                return  # 如果文件不存在，直接返回，不执行后续代码

            im1 = Image.open(file_path)
            if im1.width >= (im1.height * 2):  # 图片长宽比例超过2:1
                im2 = im1.resize((int(80 * im1.width / im1.height), 80))
                Img_m = int(im2.width / 2)
                box = ((Img_m - 80, 0, Img_m + 80, 80))  # 定义需要裁剪的空间
                im2 = im2.crop(box)
            else:
                im2 = im1.resize((160, int(160 * im1.height / im1.width)))
                Img_m = int(im2.height / 2)
                box = ((0, Img_m - 40, 160, Img_m + 40))  # 定义需要裁剪的空间
                im2 = im2.crop(box)
            im2 = im2.convert('RGB')  # 转换为RGB格式
            for y in range(0, 80):  # 逐字解析编码
                for x in range(0, 160):  # 逐字解析编码
                    r, g, b = im2.getpixel((x, y))
                    Img_data_use.append(((r >> 3) << 3) | (g >> 5))
                    Img_data_use.append((((g % 32) >> 2) << 5) | (b >> 3))

        u_time = time.time() - u_time
        u_time = int(u_time * 1000)
        Text1.insert(END, '转换完成,耗时' + str(u_time) + 'ms\n')  # 在文本框开始位置插入“内容一”
        write_path4 = 1
    else:
        Text1.insert(END, '转换失败' + 'ms\n')  # 在文本框开始位置插入“内容一”


def Page_UP():  # 上一页
    global State_change, State_machine
    State_machine = State_machine + 1
    State_change = 1
    if State_machine > 5:
        if State_machine == 6:
            State_machine = 3901
        elif State_machine == 3902:
            State_machine = 0
    print("Current state changed to: " + str(State_machine))



def Page_Down():  # 下一页
    global State_change, State_machine
    State_machine = State_machine - 1
    State_change = 1
    if State_machine < 0:
        State_machine = 3901
    if State_machine == 3900:
        State_machine = 5
    print("Current state changed to: " + str(State_machine))

# State_change = 0  # 全局变量初始化
# def now_the_state_machine():
#     if State_change == 1:
#         print(str(State_machine))
#
# now_the_state_machine()


def LCD_Change():  # 切换显示方向
    global LCD_Change_use
    LCD_Change_use = LCD_Change_use + 1
    if LCD_Change_use > 1:  # 限制切换模式
        LCD_Change_use = 0


def SER_Write(Data_U0):
    global Device_State
    # print('发送数据ing');
    try:  # 尝试发出指令,有两种无法正确发送命令的情况：1.设备被移除,发送出错；2.设备处于MSN连接状态，对于电脑发送的指令响应迟缓
        # 进行超时检测
        # u_time=time.time()
        if (False == ser.is_open):
            Device_State = 0  # 恢复到未连接状态
        ser.write(Data_U0)
        # print(Data_U0)
        # u_time=time.time()-u_time
        # if u_time>2:
        # print('发送超时');
        # Device_State=0#恢复到未连接状态
        # ser.close()#将串口关闭，防止下次无法打开
        # else:
        # print('发送完成');
    except:  # 出现异常
        # print('发送异常');
        Device_State = 0
        ser.close()  # 将串口关闭，防止下次无法打开


def SER_Read():
    global Device_State
    # print('接收数据ing');
    try:  # 尝试获取数据
        Data_U1 = ser.read(ser.in_waiting)
        return Data_U1
    except:  # 出现异常
        # print('接收异常');
        Device_State = 0
        ser.close()  # 将串口关闭，防止下次无法打开
        return 0


def Read_M_u8(add):  # 读取主机u8寄存器（MSC设备编码，Add）
    hex_use = bytearray()  # 空数组
    hex_use.append(0)  # 发给主机
    hex_use.append(48)  # 识别为SFR指令
    hex_use.append(0 * 32)  # 识别为8bit SFR读
    hex_use.append(add // 256)  # 高地址
    hex_use.append(add % 256)  # 低地址
    hex_use.append(0)  # 数值
    SER_Write(hex_use)  # 发出指令

    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("byte")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            return recv[5]


def Read_M_u16(add):  # 读取主机u8寄存器（MSC设备编码，Add）
    hex_use = bytearray()  # 空数组
    hex_use.append(0)  # 发给主机
    hex_use.append(48)  # 识别为SFR指令
    hex_use.append(1 * 32)  # 识别为16bit SFR读
    hex_use.append(add % 256)  # 地址
    hex_use.append(0)  # 高位数值
    hex_use.append(0)  # 低位数值
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("gbk")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            return recv[4] * 256 + recv[5]


def Write_M_u8(add, data_w):  # 读取主机u8寄存器（MSC设备编码，Add）
    hex_use = bytearray()  # 空数组
    hex_use.append(0)  # 发给主机
    hex_use.append(48)  # 识别为SFR指令
    hex_use.append(4 * 32)  # 识别为16bit SFR写
    hex_use.append(add // 256)  # 高地址
    hex_use.append(add % 256)  # 低地址
    hex_use.append(data_w % 256)  # 数值
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            break
            # return recv[5]


def Write_M_u16(add, data_w):  # 读取主机u8寄存器（MSC设备编码，Add）
    hex_use = bytearray()  # 空数组
    hex_use.append(0)  # 发给主机
    hex_use.append(48)  # 识别为SFR指令
    hex_use.append(1 * 32)  # 识别为16bit SFR写
    hex_use.append(add % 256)  # 地址
    hex_use.append(data_w // 256)  # 高位数值
    hex_use.append(data_w % 256)  # 低位数值
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("gbk")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            break


def Read_ADC_CH(ch):  # 读取主机ADC寄存器数值（ADC通道）
    hex_use = bytearray()  # 空数组
    hex_use.append(8)  # 读取ADC
    hex_use.append(ch)  # 通道
    hex_use.append(0)
    hex_use.append(0)
    hex_use.append(0)
    hex_use.append(0)
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("gbk")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            return recv[4] * 256 + recv[5]


def Read_M_SFR_Data(add):  # 从u8区域获取SFR描述
    SFR_data = bytearray()  # 空数组
    for i in range(0, 256):  # 以128字节为单位进行解析编码
        SFR_data.append(Read_M_u8(add + i))  # 读取编码数据
    data_type = 0  # 根据是否为0进行类型循环统计
    data_num = 0
    data_len = 0
    data_use = bytearray()  # 空数组
    data_name = b''
    data_unit = b''
    data_family = b''
    data_data = b''
    for i in range(0, 256):  # 以128字节为单位进行解析编码
        if (SFR_data[i] != 0 and data_type < 3):
            data_use.append(SFR_data[i])  # 将非0数据合并到一块
        elif (data_type < 3):  # 检测到0且未超纲
            if (len(data_use) == 0):  # 没有接收到数据时就接收到00
                break  # 检测到0后收集的数据为空，判断为结束
            if (data_type == 0):
                data_name = data_use  # 名称
                data_type = 1
            elif (data_type == 1):
                data_unit = data_use  # 单位
                data_type = 2
            elif (data_type == 2):
                data_family = data_use  # 类型
                data_type = 3
                if (int(ord(data_use) // 32) == 0):  # u8 data 2B add
                    data_len = 2
                elif (int(ord(data_use) // 32) == 1):  # u16 data 1B add
                    data_len = 1
                elif (int(ord(data_use) // 32) == 2):  # u32 data 2B add
                    data_len = 2
                elif (int(ord(data_use) // 32) == 3):  # u8 Text XB data
                    data_len = data_family[0] % 32  # 计算数据长度
            data_use = bytearray()  # 空数组
            continue  # 进行下一次循环
        if (data_len > 0 and data_type == 3):  # 正式的有效数据
            data_use.append(SFR_data[i])  # 将非0数据合并到一块
            data_len = data_len - 1
        if (data_len == 0 and data_type == 3):  # 将后续数据收集完整
            data_data = data_use
            data_type = 0  # 重置类型
            My_MSN_Data.append(MSN_Data(data_name, data_unit, data_family, data_data))  # 对数据进行登记
            data_use = bytearray()  # 空数组


def Print_MSN_Data():
    num = len(My_MSN_Data)
    data_str = ''
    print('MSN数据总数为：' + str(num))
    # 进行数据解析
    for i in range(0, num):  # 将数据全部打印出来
        data_str = data_str + '序号：' + str(i) + '    名称：' + str(My_MSN_Data[i].name) + '    单位:' + str(
            My_MSN_Data[i].unit)
        if (ord(My_MSN_Data[i].family) // 32 == 0):  # 数据类型为u8地址(16bit)
            data_str = data_str + '    类型：u8_SFR地址,长度' + str(ord(My_MSN_Data[i].family) % 32)
            data_str = data_str + '    地址：' + str(int(My_MSN_Data[i].data[0]) * 256 + int(My_MSN_Data[i].data[1]))
        elif (ord(My_MSN_Data[i].family) // 32 == 1):  # 数据类型为u16地址(8bit)
            data_str = data_str + '    类型：u16_SFR地址,长度' + str(ord(My_MSN_Data[i].family) % 32)
            data_str = data_str + '    地址：' + str(int(My_MSN_Data[i].data[0]))
        elif (ord(My_MSN_Data[i].family) // 32 == 2):  # 数据类型为u32地址(16bit)
            data_str = data_str + '    类型：u32_SFR地址,长度：' + str(ord(My_MSN_Data[i].family) % 32)
            data_str = data_str + '    地址：' + str(int(My_MSN_Data[i].data[0]) * 256 + int(My_MSN_Data[i].data[1]))
        elif (ord(My_MSN_Data[i].family) // 32 == 3):  # 数据类型为u8字符串
            data_str = data_str + '    类型：字符串,长度' + str(ord(My_MSN_Data[i].family) % 32)
            data_str = data_str + '    数据：' + str(My_MSN_Data[i].data)
        elif (ord(My_MSN_Data[i].family) // 32 == 4):  # 数据类型为u8数组
            data_str = data_str + '    类型：u8数组数据,长度' + str(int(My_MSN_Data[i].family) % 32)
            data_str = data_str + '    数据：' + str(My_MSN_Data[i].data)
        print(data_str)
        data_str = ''


def Read_MSN_Data(name_use):  # 读取MSN_data中的数据
    num = len(My_MSN_Data)
    use_data = []  # 创建一个空列表
    for i in range(0, num):  # 将数据查找一遍
        if (My_MSN_Data[i].name == name_use):
            if (ord(My_MSN_Data[i].family) // 32 == 0):  # 数据类型为u8地址(16bit)
                sfr_add = int(My_MSN_Data[i].data[0]) * 256 + int(My_MSN_Data[i].data[1])
                for n in range(0, ord(My_MSN_Data[i].family) % 32):
                    use_data.append(Read_M_u8(sfr_add + n))
            elif (ord(My_MSN_Data[i].family) // 32 == 1):  # 数据类型为u16地址(8bit)
                use_data = Read_M_u16(int(My_MSN_Data[i].data[0]))
            elif (ord(My_MSN_Data[i].family) // 32 == 3):  # 数据类型为u8字符串
                use_data = My_MSN_Data[i].data
            elif (ord(My_MSN_Data[i].family) // 32 == 4):  # 数据类型为u8数组
                use_data = My_MSN_Data[i].data
            print(str(My_MSN_Data[i].name) + '=' + str(use_data))
            return use_data
    if name_use != 0:
        print('"' + name_use + '"' + '不存在,请检查名称是否正确')
    return 0


def Write_MSN_Data(name_use, data_w):  # 在MSN_data写入数据
    num = len(My_MSN_Data)
    for i in range(0, num):  # 将数据查找一遍
        if (My_MSN_Data[i].name == name_use):
            if (int(My_MSN_Data[i].family) // 32 == 0):  # 数据类型为u8地址(16bit)
                Write_M_u8(int(My_MSN_Data[i].data[0]) * 256 + int(My_MSN_Data[i].data[1]), data_w)
                print('"' + name_use + '"' + '写入' + str(data_w) + '完成')
                return 0
            elif (int(My_MSN_Data[i].family) // 32 == 1):  # 数据类型为u16地址(8bit)
                Write_M_u16(int(My_MSN_Data[i].data[0]), data_w)
                print('"' + name_use + '"' + '写入' + str(data_w) + '完成')
                return 0
    print('"' + name_use + '"' + '不存在,请检查名称是否正确')


def Write_Flash_Page(Page_add, data_w, Page_num):  # 往Flash指定页写入256B数据
    # 先把数据传输完成
    hex_use = bytearray()  # 空数组
    for i in range(0, 64):  # 256字节数据分为64个指令
        hex_use.append(4)  # 多次写入Flash
        hex_use.append(i)  # 低位地址
        hex_use.append(data_w[i * 4 + 0])  # Data0
        hex_use.append(data_w[i * 4 + 1])  # Data1
        hex_use.append(data_w[i * 4 + 2])  # Data2
        hex_use.append(data_w[i * 4 + 3])  # Data3
        SER_Write(hex_use)  # 发出指令
    hex_use = bytearray()  # 空数组
    hex_use.append(3)  # 对Flash操作
    hex_use.append(1)  # 写Flash
    hex_use.append(Page_add // (65536))  # Data0
    hex_use.append((Page_add % 65536) // 256)  # Data1
    hex_use.append((Page_add % 65536) % 256)  # Data2
    hex_use.append(Page_num % 256)  # Data3
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            break


def Write_Flash_Page_fast(Page_add, data_w, Page_num):  # 未经过擦除，直接往Flash指定页写入256B数据
    # 先把数据传输完成
    hex_use = b''
    for i in range(0, 64):  # 256字节数据分为64个指令
        hex_use = hex_use + int(4).to_bytes(1, byteorder="little")  # 多次写入Flash
        hex_use = hex_use + int(i).to_bytes(1, byteorder="little")  # 低位地址
        hex_use = hex_use + data_w[i * 4 + 0].to_bytes(1, byteorder="little")  # Data0
        hex_use = hex_use + data_w[i * 4 + 1].to_bytes(1, byteorder="little")  # Data1
        hex_use = hex_use + data_w[i * 4 + 2].to_bytes(1, byteorder="little")  # Data2
        hex_use = hex_use + data_w[i * 4 + 3].to_bytes(1, byteorder="little")  # Data3
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 对Flash操作
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 经过擦除，写Flash
    hex_use = hex_use + int(Page_add // (256 * 256)).to_bytes(1, byteorder="little")  # Data0
    hex_use = hex_use + int((Page_add % 65536) // 256).to_bytes(1, byteorder="little")  # Data1
    hex_use = hex_use + int((Page_add % 65536) % 256).to_bytes(1, byteorder="little")  # Data2
    hex_use = hex_use + int(Page_num).to_bytes(1, byteorder="little")  # Data3
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            break


def Erase_Flash_page(add, size):  # 清空指定区域的内存
    hex_use = bytearray()  # 空数组
    hex_use.append(3)  # 对Flash操作
    hex_use.append(2)  # 清空指定区域的内存
    hex_use.append((add % 65536) // 256)  # Data1
    hex_use.append((add % 65536) % 256)  # Data2
    hex_use.append((size % 65536) // 256)  # Data1
    hex_use.append((size % 65536) % 256)  # Data2
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            break


def Read_Flash_byte(add):  # 读取指定地址的数值
    hex_use = bytearray()  # 空数组
    hex_use.append(3)  # 对Flash操作
    hex_use.append(0)  # 读Flash
    hex_use.append(add // (256 * 256))  # Data0
    hex_use.append((add % 65536) // 256)  # Data1
    hex_use.append((add % 65536) % 256)  # Data2
    hex_use.append(0)  # Data3
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            print(recv[5])
            return recv[5]


def Write_Flash_Photo_fast(Page_add, Photo_name):  # 往Flash里面写入Bin格式的照片
    global Text1
    filepath = Photo_name + '.bin'  # 合成文件名称
    try:  # 尝试打开bin文件
        binfile = open(filepath, 'rb')  # 以只读方式打开
    except:  # 出现异常
        print('找不到“' + filepath + '”文件,请检查其位置是否位于当前目录下')
        # Text1.delete(1.0,END)#清除文本框
        Text1.insert(END, '文件路径或格式出错!\n')  # 在文本框开始位置插入“内容一”
        return 0
    Fsize = os.path.getsize(filepath)
    print('找到“' + filepath + '”文件,大小：' + str(Fsize) + ' B')
    Text1.insert(END, '大小' + str(Fsize) + 'B,烧录中...\n')  # 在文本框开始位置插入“内容一”
    u_time = time.time()
    # 进行擦除
    if (Fsize % 256 != 0):
        Erase_Flash_page(Page_add, Fsize // 256 + 1)  # 清空指定区域的内存
    else:
        Erase_Flash_page(Page_add, Fsize // 256)  # 清空指定区域的内存

    for i in range(0, Fsize // 256):  # 每次写入一个Page
        Fdata = binfile.read(256)
        Write_Flash_Page_fast(Page_add + i, Fdata, 1)  # (page,数据，大小)
    if (Fsize % 256 != 0):  # 还存在没写完的数据
        Fdata = binfile.read(Fsize % 256)  # 将剩下的数据读完
        for i in range(Fsize % 256, 256):
            Fdata = Fdata + int(255).to_bytes(1, byteorder="little")  # 不足位置补充0xFF
        Write_Flash_Page_fast(Page_add + Fsize // 256, Fdata, 1)  # (page,数据，大小)
    u_time = time.time() - u_time
    print(filepath + ' 烧写完成,耗时' + str(u_time) + '秒')
    Text1.insert(END, '烧写完成,耗时' + str(int(u_time * 1000)) + 'ms\n')  # 在文本框开始位置插入“内容一”


def Write_Flash_hex_fast(Page_add, img_use):  # 往Flash里面写入hex数据
    Fsize = len(img_use)
    Text1.insert(END, '大小' + str(Fsize) + 'B,烧录中...\n')  # 在文本框开始位置插入“内容一”
    u_time = time.time()
    # 进行擦除
    if (Fsize % 256 != 0):
        Erase_Flash_page(Page_add, Fsize // 256 + 1)  # 清空指定区域的内存
    else:
        Erase_Flash_page(Page_add, Fsize // 256)  # 清空指定区域的内存
    for i in range(0, Fsize // 256):  # 每次写入一个Page
        Fdata = img_use[:256]  # 取前256字节
        img_use = img_use[256:]  # 取剩余字节
        Write_Flash_Page_fast(Page_add + i, Fdata, 1)  # (page,数据，大小)
    if (Fsize % 256 != 0):  # 还存在没写完的数据
        Fdata = img_use  # 将剩下的数据读完
        for i in range(Fsize % 256, 256):
            Fdata = Fdata + int(255).to_bytes(1, byteorder="little")  # 不足位置补充0xFF
        Write_Flash_Page_fast(Page_add + Fsize // 256, Fdata, 1)  # (page,数据，大小)
    u_time = time.time() - u_time
    Text1.insert(END, '烧写完成,耗时' + str(int(u_time * 1000)) + 'ms\n')  # 在文本框开始位置插入“内容一”


def Write_Flash_ZK(Page_add, ZK_name):  # 往Flash里面写入Bin格式的字库
    filepath = ZK_name + '.bin'  # 合成文件名称
    try:  # 尝试打开bin文件
        binfile = open(filepath, 'rb')  # 以只读方式打开
    except:  # 出现异常
        print('找不到“' + filepath + '”文件,请检查其位置是否位于当前目录下')
        return 0
    Fsize = os.path.getsize(filepath) - 6  # 字库文件的最后六个字节不是点阵信息
    print('找到“' + filepath + '”文件,大小：' + str(Fsize) + ' B')
    for i in range(0, Fsize // 256):  # 每次写入一个Pag
        Fdata = binfile.read(256)
        Write_Flash_Page(Page_add + i, Fdata, 1)  # (page,数据，大小)
    if (Fsize % 256 != 0):  # 还存在没写完的数据
        Fdata = binfile.read(Fsize % 256)  # 将剩下的数据读完
        for i in range(Fsize % 256, 256):
            Fdata = Fdata + int(255).to_bytes(1, byteorder="little")  # 不足位置补充0xFF
        Write_Flash_Page(Page_add + Fsize // 256, Fdata, 1)  # (page,数据，大小)
    print(filepath + ' 烧写完成')


def LCD_Set_XY(LCD_D0, LCD_D1):  # 设置起始位置
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")  # 设置起始位置
    hex_use = hex_use + int(LCD_D0 // 256).to_bytes(1, byteorder="little")  # Data0
    hex_use = hex_use + int(LCD_D0 % 256).to_bytes(1, byteorder="little")  # Data1
    hex_use = hex_use + int(LCD_D1 // 256).to_bytes(1, byteorder="little")  # Data2
    hex_use = hex_use + int(LCD_D1 % 256).to_bytes(1, byteorder="little")  # Data3
    SER_Write(hex_use)  # 发出指令


def LCD_Set_Size(LCD_D0, LCD_D1):  # 设置大小
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(1).to_bytes(1, byteorder="little")  # 设置大小
    hex_use = hex_use + int(LCD_D0 // 256).to_bytes(1, byteorder="little")  # Data0
    hex_use = hex_use + int(LCD_D0 % 256).to_bytes(1, byteorder="little")  # Data1
    hex_use = hex_use + int(LCD_D1 // 256).to_bytes(1, byteorder="little")  # Data2
    hex_use = hex_use + int(LCD_D1 % 256).to_bytes(1, byteorder="little")  # Data3
    SER_Write(hex_use)  # 发出指令


def LCD_Set_Color(LCD_D0, LCD_D1):  # 设置颜色（FC,BC）
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(2).to_bytes(1, byteorder="little")  # 设置颜色
    hex_use = hex_use + int(LCD_D0 // 256).to_bytes(1, byteorder="little")  # Data0
    hex_use = hex_use + int(LCD_D0 % 256).to_bytes(1, byteorder="little")  # Data1
    hex_use = hex_use + int(LCD_D1 // 256).to_bytes(1, byteorder="little")  # Data2
    hex_use = hex_use + int(LCD_D1 % 256).to_bytes(1, byteorder="little")  # Data3
    SER_Write(hex_use)  # 发出指令


def LCD_Photo(LCD_X, LCD_Y, LCD_X_Size, LCD_Y_Size, Page_Add):  #
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Size(LCD_X_Size, LCD_Y_Size)
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")  # 显示彩色图片
    hex_use = hex_use + int(Page_Add // 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Page_Add % 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.001)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_ADD(LCD_X, LCD_Y, LCD_X_Size, LCD_Y_Size):  #
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Size(LCD_X_Size, LCD_Y_Size)
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(7).to_bytes(1, byteorder="little")  # 载入地址
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.001)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_State(LCD_S):  #
    global Device_State
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(10).to_bytes(1, byteorder="little")  # 载入地址
    hex_use = hex_use + int(LCD_S).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.001)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_DATA(data_w, size):  # 往LCD写入指定大小的数据
    # 先把数据传输完成
    hex_use = b''
    for i in range(0, 64):  # 256字节数据分为64个指令
        hex_use = hex_use + int(4).to_bytes(1, byteorder="little")  # 多次写入Flash
        hex_use = hex_use + int(i).to_bytes(1, byteorder="little")  # 低位地址
        hex_use = hex_use + data_w[i * 4 + 0].to_bytes(1, byteorder="little")  # Data0
        hex_use = hex_use + data_w[i * 4 + 1].to_bytes(1, byteorder="little")  # Data1
        hex_use = hex_use + data_w[i * 4 + 2].to_bytes(1, byteorder="little")  # Data2
        hex_use = hex_use + data_w[i * 4 + 3].to_bytes(1, byteorder="little")  # Data3
    hex_use = hex_use + int(2).to_bytes(1, byteorder="little")  # 对Flash操作
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 经过擦除，写Flash
    hex_use = hex_use + int(8).to_bytes(1, byteorder="little")  # Data0
    hex_use = hex_use + int(size // 256).to_bytes(1, byteorder="little")  # Data1
    hex_use = hex_use + int(size % 256).to_bytes(1, byteorder="little")  # Data2
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")  # Data3
    SER_Write(hex_use)  # 发出指令


def Write_LCD_Photo_fast(x_star, y_star, x_size, y_size, Photo_name):  # 往Flash里面写入Bin格式的照片
    filepath = Photo_name + '.bin'  # 合成文件名称
    try:  # 尝试打开bin文件
        binfile = open(filepath, 'rb')  # 以只读方式打开
    except:  # 出现异常
        print('找不到“' + filepath + '”文件,请检查其位置是否位于当前目录下')
        return 0
    Fsize = os.path.getsize(filepath)
    print('找到“' + filepath + '”文件,大小：' + str(Fsize) + ' B')
    u_time = time.time()
    # 进行地址写入
    LCD_ADD(x_star, y_star, x_size, y_size)
    for i in range(0, Fsize // 256):  # 每次写入一个Page
        Fdata = binfile.read(256)
        LCD_DATA(Fdata, 256)  # (page,数据，大小)
    if (Fsize % 256 != 0):  # 还存在没写完的数据
        Fdata = binfile.read(Fsize % 256)  # 将剩下的数据读完
        for i in range(Fsize % 256, 256):
            Fdata = Fdata + int(255).to_bytes(1, byteorder="little")  # 不足位置补充0xFF
        LCD_DATA(Fdata, Fsize % 256)  # (page,数据，大小)
    u_time = time.time() - u_time
    print(filepath + ' 显示完成,耗时' + str(u_time) + '秒')


def Write_LCD_Photo_fast1(x_star, y_star, x_size, y_size, Photo_name):  # 往Flash里面写入Bin格式的照片
    filepath = Photo_name + '.bin'  # 合成文件名称
    try:  # 尝试打开bin文件
        binfile = open(filepath, 'rb')  # 以只读方式打开
    except:  # 出现异常
        print('找不到“' + filepath + '”文件,请检查其位置是否位于当前目录下')
        return 0
    Fsize = os.path.getsize(filepath)
    print('找到“' + filepath + '”文件,大小：' + str(Fsize) + ' B')
    u_time = time.time()
    # 进行地址写入
    LCD_ADD(x_star, y_star, x_size, y_size)
    hex_use = bytearray()  # 空数组
    for j in range(0, Fsize // 256):  # 每次写入一个Page
        data_w = binfile.read(256)
        # 先把数据格式转换好
        for i in range(0, 64):  # 256字节数据分为64个指令
            hex_use.append(4)
            hex_use.append(i)
            hex_use.append(data_w[i * 4 + 0])
            hex_use.append(data_w[i * 4 + 1])
            hex_use.append(data_w[i * 4 + 2])
            hex_use.append(data_w[i * 4 + 3])
        hex_use.append(2)
        hex_use.append(3)
        hex_use.append(8)
        hex_use.append(1)
        hex_use.append(0)
        hex_use.append(0)
    if (Fsize % 256 != 0):  # 还存在没写完的数据
        data_w = binfile.read(Fsize % 256)  # 将剩下的数据读完
        for i in range(Fsize % 256, 256):
            data_w = data_w + int(255).to_bytes(1, byteorder="little")  # 不足位置补充0xFF
        for i in range(0, 64):  # 256字节数据分为64个指令
            hex_use.append(4)
            hex_use.append(i)
            hex_use.append(data_w[i * 4 + 0])
            hex_use.append(data_w[i * 4 + 1])
            hex_use.append(data_w[i * 4 + 2])
            hex_use.append(data_w[i * 4 + 3])
        hex_use.append(2)
        hex_use.append(3)
        hex_use.append(8)
        hex_use.append(0)
        hex_use.append(Fsize % 256)
        hex_use.append(0)
    hex_use.append(2)
    hex_use.append(3)
    hex_use.append(9)
    hex_use.append(0)
    hex_use.append(0)
    hex_use.append(0)
    SER_Write(hex_use)  # 发出指令
    u_time = time.time() - u_time
    print(filepath + ' 显示完成,耗时' + str(u_time) + '秒')


def Write_LCD_Screen_fast(x_star, y_star, x_size, y_size, Photo_data):  # 往Flash里面写入Bin格式的照片
    LCD_ADD(x_star, y_star, x_size, y_size)
    Photo_data_use = Photo_data
    hex_use = bytearray()  # 空数组
    for j in range(0, x_size * y_size * 2 // 256):  # 每次写入一个Page
        data_w = Photo_data_use[:256]
        Photo_data_use = Photo_data_use[256:]
        cmp_use = []  # 空数组,
        for i in range(0, 64):  # 256字节数据分为64个指令
            cmp_use.append(
                data_w[i * 4 + 0] * 256 * 256 * 256 + data_w[i * 4 + 1] * 256 * 256 + data_w[i * 4 + 2] * 256 + data_w[
                    i * 4 + 3])
        result = max(set(cmp_use), key=cmp_use.count)  # 统计出现最多的数据
        hex_use.append(2)
        hex_use.append(4)
        color_ram = result
        hex_use.append(color_ram // (256 * 256 * 256))
        color_ram = color_ram % (256 * 256 * 256)
        hex_use.append(color_ram // (256 * 256))
        color_ram = color_ram % (256 * 256)
        hex_use.append(color_ram // 256)
        hex_use.append(color_ram % 256)
        # 先把数据格式转换好
        for i in range(0, 64):  # 256字节数据分为64个指令
            if ((data_w[i * 4 + 0] * 256 * 256 * 256 + data_w[i * 4 + 1] * 256 * 256 + data_w[i * 4 + 2] * 256 + data_w[
                i * 4 + 3]) != result):  #
                hex_use.append(4)
                hex_use.append(i)
                hex_use.append(data_w[i * 4 + 0])
                hex_use.append(data_w[i * 4 + 1])
                hex_use.append(data_w[i * 4 + 2])
                hex_use.append(data_w[i * 4 + 3])
        hex_use.append(2)
        hex_use.append(3)
        hex_use.append(8)
        hex_use.append(1)
        hex_use.append(0)
        hex_use.append(0)
    if (x_size * y_size * 2 % 256 != 0):  # 还存在没写完的数据
        data_w = Photo_data_use  # 将剩下的数据读完
        for i in range(x_size * y_size * 2 % 256, 256):
            data_w.append(0xff)  # 不足位置补充0xFF
        for i in range(0, 64):  # 256字节数据分为64个指令
            hex_use.append(4)
            hex_use.append(i)
            hex_use.append(data_w[i * 4 + 0])
            hex_use.append(data_w[i * 4 + 1])
            hex_use.append(data_w[i * 4 + 2])
            hex_use.append(data_w[i * 4 + 3])
        hex_use.append(2)
        hex_use.append(3)
        hex_use.append(8)
        hex_use.append(0)
        hex_use.append(x_size * y_size * 2 % 256)
        hex_use.append(0)
    SER_Write(hex_use)  # 发出指令


# 对发送的数据进行编码分析,缩短数据指令
def Write_LCD_Screen_fast1(x_star, y_star, x_size, y_size, Photo_data):  # 往Flash里面写入Bin格式的照片
    LCD_ADD(x_star, y_star, x_size, y_size)
    Photo_data_use = Photo_data
    hex_use = bytearray()  # 空数组
    for j in range(0, x_size * y_size * 2 // 256):  # 每次写入一个Page
        data_w = Photo_data_use[:256]
        Photo_data_use = Photo_data_use[256:]
        # 先把数据格式转换好
        for i in range(0, 64):  # 256字节数据分为64个指令
            hex_use.append(4)
            hex_use.append(i)
            hex_use.append(data_w[i * 4 + 0])
            hex_use.append(data_w[i * 4 + 1])
            hex_use.append(data_w[i * 4 + 2])
            hex_use.append(data_w[i * 4 + 3])
        hex_use.append(2)
        hex_use.append(3)
        hex_use.append(8)
        hex_use.append(1)
        hex_use.append(0)
        hex_use.append(0)
    if (x_size * y_size * 2 % 256 != 0):  # 还存在没写完的数据
        data_w = Photo_data_use  # 将剩下的数据读完
        for i in range(x_size * y_size * 2 % 256, 256):
            data_w.append(0xff)  # 不足位置补充0xFF
        for i in range(0, 64):  # 256字节数据分为64个指令
            hex_use.append(4)
            hex_use.append(i)
            hex_use.append(data_w[i * 4 + 0])
            hex_use.append(data_w[i * 4 + 1])
            hex_use.append(data_w[i * 4 + 2])
            hex_use.append(data_w[i * 4 + 3])
        hex_use.append(2)
        hex_use.append(3)
        hex_use.append(8)
        hex_use.append(0)
        hex_use.append(x_size * y_size * 2 % 256)
        hex_use.append(0)
    # 等待传输完成
    hex_use.append(2)
    hex_use.append(3)
    hex_use.append(9)
    hex_use.append(0)
    hex_use.append(0)
    hex_use.append(0)
    SER_Write(hex_use)  # 发出指令


def LCD_Photo_wb(LCD_X, LCD_Y, LCD_X_Size, LCD_Y_Size, Page_Add, LCD_FC, LCD_BC):  #
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Size(LCD_X_Size, LCD_Y_Size)
    LCD_Set_Color(LCD_FC, LCD_BC)
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(1).to_bytes(1, byteorder="little")  # 显示单色图片
    hex_use = hex_use + int(Page_Add // 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Page_Add % 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.001)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):  # 对于回传的数据需要进行校验，确保设备状态能够被准确识别到
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_ASCII_32X64(LCD_X, LCD_Y, Txt, LCD_FC, LCD_BC, Num_Page):  #
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Color(LCD_FC, LCD_BC)
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(2).to_bytes(1, byteorder="little")  # 显示ASCII
    hex_use = hex_use + int(ord(Txt)).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Num_Page // 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Num_Page % 256).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.001)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_GB2312_16X16(LCD_X, LCD_Y, Txt, LCD_FC, LCD_BC):  #
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Color(LCD_FC, LCD_BC)
    Txt_Data = Txt.encode('gb2312')
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 显示彩色图片
    hex_use = hex_use + int(Txt_Data[0]).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Txt_Data[1]).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.001)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_Photo_wb_MIX(LCD_X, LCD_Y, LCD_X_Size, LCD_Y_Size, Page_Add, LCD_FC, BG_Page):  #
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Size(LCD_X_Size, LCD_Y_Size)
    LCD_Set_Color(LCD_FC, BG_Page)
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(4).to_bytes(1, byteorder="little")  # 显示单色图片
    hex_use = hex_use + int(Page_Add // 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Page_Add % 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.001)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_ASCII_32X64_MIX(LCD_X, LCD_Y, Txt, LCD_FC, BG_Page, Num_Page):  #
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Color(LCD_FC, BG_Page)
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(5).to_bytes(1, byteorder="little")  # 显示ASCII
    hex_use = hex_use + int(ord(Txt)).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Num_Page // 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Num_Page % 256).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        # time.sleep(0.5)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_GB2312_16X16_MIX(LCD_X, LCD_Y, Txt, LCD_FC, BG_Page):  #
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Color(LCD_FC, BG_Page)
    Txt_Data = Txt.encode('gb2312')
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(6).to_bytes(1, byteorder="little")  # 显示彩色图片
    hex_use = hex_use + int(Txt_Data[0]).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(Txt_Data[1]).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.2)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def LCD_Color_set(LCD_X, LCD_Y, LCD_X_Size, LCD_Y_Size, F_Color):  # 对指定区域进行颜色填充
    global Device_State
    LCD_Set_XY(LCD_X, LCD_Y)
    LCD_Set_Size(LCD_X_Size, LCD_Y_Size)
    hex_use = int(2).to_bytes(1, byteorder="little")  # 对LCD多次写入
    hex_use = hex_use + int(3).to_bytes(1, byteorder="little")  # 设置指令
    hex_use = hex_use + int(11).to_bytes(1, byteorder="little")  # 显示彩色图片
    hex_use = hex_use + int(F_Color // 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(F_Color % 256).to_bytes(1, byteorder="little")
    hex_use = hex_use + int(0).to_bytes(1, byteorder="little")
    SER_Write(hex_use)  # 发出指令
    # 等待收回信息
    while (1):
        time.sleep(0.001)
        recv = SER_Read()  # .decode("UTF-8")#获取串口数据
        if (recv == 0):
            return 0
        elif (len(recv) != 0):
            if ((recv[0] != hex_use[0]) or (recv[1] != hex_use[1])):
                Device_State = 0  # 接收出错
            break


def show_gif():  # 显示GIF动图
    global State_change, gif_num
    if (State_change == 1):
        State_change = 0
        gif_num = 0
    if (State_change == 0):
        LCD_Photo(0, 0, 160, 80, gif_num * 100)
        gif_num = gif_num + 1
        if (gif_num > 35):
            gif_num = 0

    time.sleep(0.05)
    # LCD_Color_set(40,0,80,80,RED)
state=1
def get_gpu_temp():
    global state
    rate,temp="",""
    window_title = "TechPowerUp GPU-Z 2.57.0"
    t1=autoit.win_get_title(window_title)
    if state!=0:
        state=0
        print(t1)
    # time.sleep(1)
    # 隐藏窗口
    autoit.win_set_state(t1,0)
    txt=autoit.win_get_text(t1)
    if gpu_num==1:
        txt=autoit.control_get_text(t1,"Static83")#使用率
    else:
        txt=autoit.control_get_text(t1,"Static77")#使用率
    # print(txt)
    rate=txt
    txt=autoit.control_get_text(t1,"Static74")#温度
    # print(txt)
    temp=txt
    return rate#,temp

def write_BG(text_color=(255, 128, 0)):
    im1 = Image.new('RGB', (size_USE_X1, size_USE_Y1), (0, 0, 0))
    # im1 = Image.open('示例.png')
    
    draw = ImageDraw.Draw(im1)
    font_size = 18
    try:
        font = ImageFont.truetype("simhei.ttf", font_size)
    except OSError:
        # Pillow 可能不能忽略文件大小写，以免读取失败
        font = ImageFont.truetype("SimHei.ttf", font_size)
    
    # 绘制文字
    
    text = "核"
    draw.text((5, 0), text, fill=text_color, font=font)
    text = "心"
    draw.text((5, 16), text, fill=text_color, font=font)
    
    text = "内"
    draw.text((80, 0), text, fill=text_color, font=font)
    text = "存"
    draw.text((80, 16), text, fill=text_color, font=font)
    
    
    text = "显"
    draw.text((5, 46), text, fill=text_color, font=font)
    text = "卡"
    draw.text((5, 62), text, fill=text_color, font=font)


    text = "电"
    draw.text((80, 46), text, fill=text_color, font=font)
    text = "池"
    draw.text((80, 62), text, fill=text_color, font=font)

    im2 = im1.load()
    
    hex_16RGB = []  # bytearray()
    
    for y in range(0, size_USE_Y1):
        for x in range(0, size_USE_X1):
            hex_16RGB.append(
                ((im2[x, y][0] >> 3) << 11) | ((im2[x, y][1] >> 2) << 5) | (im2[x, y][2] >> 3))  # 先直接添加16bit数组
    hexstream = Screen_Date_Process(hex_16RGB)
    LCD_ADD((160 - size_USE_X1) // 2, (80 - size_USE_Y1) // 2, size_USE_X1, size_USE_Y1)
    SER_Write(hexstream)

def show_PC_state(FC, BC):  # 显示PC状态
    global State_change
    photo_add = 4038
    num_add = 4026
    if (State_change == 1):
        State_change = 0
        # LCD_Photo_wb(0, 0, 160, 80, photo_add, FC, BC)  # 放置背景
        write_BG()
    if (State_change == 0):
        CPU = int(psutil.cpu_percent(interval=0.5))
        mem = psutil.virtual_memory()
        RAM = int(mem.percent)

        battery = psutil.sensors_battery()
        if battery != None:
            BAT = int(battery.percent)
        else:
            BAT = 100
        # FRQ = int(psutil.disk_usage('/').used * 100 / psutil.disk_usage('/').total)
        FRQ=int(re.match(r"\d{1,3}",get_gpu_temp()).group(0))
        
        if (CPU >= 100):
            LCD_Photo_wb(24, 0, 8, 33, 10 + num_add, FC, BC)
            CPU = CPU % 100
        else:
            LCD_Photo_wb(24, 0, 8, 33, 11 + num_add, FC, BC)
        LCD_Photo_wb(32, 0, 24, 33, (CPU // 10) + num_add, FC, BC)
        LCD_Photo_wb(56, 0, 24, 33, (CPU % 10) + num_add, FC, BC)
        if (RAM >= 100):
            LCD_Photo_wb(104, 0, 8, 33, 10 + num_add, FC, BC)
            RAM = RAM % 100
        else:
            LCD_Photo_wb(104, 0, 8, 33, 11 + num_add, FC, BC)
        LCD_Photo_wb(112, 0, 24, 33, (RAM // 10) + num_add, FC, BC)
        LCD_Photo_wb(136, 0, 24, 33, (RAM % 10) + num_add, FC, BC)
        if (BAT >= 100):
            LCD_Photo_wb(104, 47, 8, 33, 10 + num_add, FC, BC)
            BAT = BAT % 100
        else:
            LCD_Photo_wb(104, 47, 8, 33, 11 + num_add, FC, BC)
        LCD_Photo_wb(112, 47, 24, 33, (BAT // 10) + num_add, FC, BC)
        LCD_Photo_wb(136, 47, 24, 33, (BAT % 10) + num_add, FC, BC)

        if (FRQ >= 100):
            LCD_Photo_wb(24, 47, 8, 33, 10 + num_add, FC, BC)
            FRQ = FRQ % 100
        else:
            LCD_Photo_wb(24, 47, 8, 33, 11 + num_add, FC, BC)
        LCD_Photo_wb(32, 47, 24, 33, (FRQ // 10) + num_add, FC, BC)
        LCD_Photo_wb(56, 47, 24, 33, (FRQ % 10) + num_add, FC, BC)


def show_Photo1():  # 显示照片
    global State_change
    FC = BLUE
    BC = BLACK
    if (State_change == 1):
        State_change = 0
        LCD_Photo(0, 0, 160, 80, 3926)  # 放置背景
    if (State_change == 0):
        time.sleep(0.2)


def show_PC_time():
    global State_change
    FC = YELLOW
    photo_add = 3826
    num_add = 3651
    if (State_change == 1):
        State_change = 0
        LCD_Photo(0, 0, 160, 80, photo_add)  # 放置背景
        # while(1):
        #    time.sleep(1)
        LCD_ASCII_32X64_MIX(56 + 8, 8, ':', FC, photo_add, num_add)
        
        # LCD_GB2312_16X16_MIX(56 + 8, 8, ':', FC, photo_add);
        
        # LCD_ASCII_32X64_MIX(136+8,32,':',FC,photo_add,num_add)
    if (State_change == 0):
        time_h = int(datetime.now().hour)
        time_m = int(datetime.now().minute)
        time_S = int(datetime.now().second)
        # LCD_GB2312_16X16_MIX(0 + 8, 8, chr((time_h // 10) + 48), FC, photo_add);
        # LCD_GB2312_16X16_MIX(32 + 8, 8, chr((time_h % 10) + 48), FC, photo_add);
        # LCD_GB2312_16X16_MIX(80 + 8, 8, chr((time_m // 10) + 48), FC, photo_add);
        # LCD_GB2312_16X16_MIX(112 + 8, 8, chr((time_m % 10) + 48), FC, photo_add);
        
        LCD_ASCII_32X64_MIX(0 + 8, 8, chr((time_h // 10) + 48), FC, photo_add, num_add)
        LCD_ASCII_32X64_MIX(32 + 8, 8, chr((time_h % 10) + 48), FC, photo_add, num_add)
        LCD_ASCII_32X64_MIX(80 + 8, 8, chr((time_m // 10) + 48), FC, photo_add, num_add)
        LCD_ASCII_32X64_MIX(112 + 8, 8, chr((time_m % 10) + 48), FC, photo_add, num_add)
        # LCD_ASCII_32X64_MIX(160+8,8,chr((time_S//10)+48),FC,photo_add,num_add)
        # LCD_ASCII_32X64_MIX(192+8,8,chr((time_S%10)+48),FC,photo_add,num_add)
        time.sleep(0.2)


def Screen_Date_Process(Photo_data):  # 对数据进行转换处理
    Photo_data_use = Photo_data
    hex_use = bytearray()  # 空数组
    for j in range(0, size_USE_X1 * size_USE_Y1 // 128):  # 每次写入一个Page
        data_w = Photo_data_use[:128]
        Photo_data_use = Photo_data_use[128:]
        cmp_use = []  # 空数组,
        for i in range(0, 64):  # 256字节数据分为64个指令
            cmp_use.append(data_w[i * 2 + 0] * 65536 + data_w[i * 2 + 1])
        result = max(set(cmp_use), key=cmp_use.count)  # 统计出现最多的数据
        hex_use.append(2)
        hex_use.append(4)
        color_ram = result
        hex_use.append(color_ram >> 24)
        color_ram = color_ram % 16777216
        hex_use.append(color_ram >> 16)
        color_ram = color_ram % 65536
        hex_use.append(color_ram >> 8)
        hex_use.append(color_ram % 256)
        # 先把数据格式转换好
        for i in range(0, 64):  # 256字节数据分为64个指令
            if ((data_w[i * 2 + 0] * 65536 + data_w[i * 2 + 1]) != result):
                hex_use.append(4)
                hex_use.append(i)
                hex_use.append(data_w[i * 2 + 0] >> 8)
                hex_use.append(data_w[i * 2 + 0] % 256)
                hex_use.append(data_w[i * 2 + 1] >> 8)
                hex_use.append(data_w[i * 2 + 1] % 256)
        hex_use.append(2)
        hex_use.append(3)
        hex_use.append(8)
        hex_use.append(1)
        hex_use.append(0)
        hex_use.append(0)
    if (size_USE_X1 * size_USE_Y1 * 2 % 256 != 0):  # 还存在没写完的数据
        data_w = Photo_data_use  # 将剩下的数据读完
        for i in range(size_USE_X1 * size_USE_Y1 * 2 % 256, 256):
            data_w.append(0xffff)  # 不足位置补充0xFFFF
        for i in range(0, 64):  # 256字节数据分为64个指令
            hex_use.append(4)
            hex_use.append(i)
            hex_use.append(data_w[i * 2 + 0] >> 8)
            hex_use.append(data_w[i * 2 + 0] % 256)
            hex_use.append(data_w[i * 2 + 1] >> 8)
            hex_use.append(data_w[i * 2 + 1] % 256)
        hex_use.append(2)
        hex_use.append(3)
        hex_use.append(8)
        hex_use.append(0)
        hex_use.append(size_USE_X1 * size_USE_Y1 * 2 % 256)
        hex_use.append(0)
    return hex_use


MG_screen_thread_should_stop = False

# 创建两个数据缓存区，防止冲突
def Screen_Date_get():  # 创建专门的函数来获取屏幕图像和处理转换数据
    global G_screnn0_OK, G_screnn1_OK, G_screnn0, G_screnn1, size_USE_X1, size_USE_Y1
    print("截图线程创建成功")
    size_PC = pyautogui.size()
    size_mode = 0
    if (size_mode == 0):  # 横向充满
        if (size_PC.width >= size_PC.height * 2):  # 极宽屏
            size_USE_X1 = 160
            size_USE_Y1 = 160 * size_PC.height // size_PC.width
        else:
            size_USE_X1 = 160
            size_USE_Y1 = 80
    elif (size_mode == 1):  # 纵向充满
        if (size_PC.height * 2 >= size_PC.width):
            size_USE_X1 = 80 * size_PC.width // size_PC.height
            size_USE_Y1 = 80
        else:
            size_USE_X1 = 160
            size_USE_Y1 = 80
    elif (size_mode == 2):  # 拉伸充满
        size_USE_X1 = 160
        size_USE_Y1 = 80
    while (1):
        if (G_screnn0_OK == 0 or G_screnn1_OK == 0):
            u_time1 = time.time()
            hex_16RGB = []  # bytearray()
            im = pyautogui.screenshot()  # 截屏需要110ms太慢了#截屏约45ms，裁剪约18ms，格式转换24ms，
            if (size_mode == 0):  # 横向充满
                if (size_PC.width >= size_PC.height * 2):
                    im1 = im.resize((size_USE_X1, size_USE_Y1))  # 进行缩放
                else:
                    im1 = im.resize((160, 160 * size_PC.height // size_PC.width))  # 进行缩放
                    im1 = im1.crop((0, (160 * size_PC.height // size_PC.width - 80) // 2, 160,
                                    (160 * size_PC.height // size_PC.width - 80) // 2 + 80))  # 进行中心裁剪#进行中心裁剪
            elif (size_mode == 1):  # 纵向充满
                if (size_PC.height * 2 >= size_PC.width):
                    im1 = im.resize((size_USE_X1, size_USE_Y1))  # 进行缩放
                else:
                    im1 = im.resize((80 * size_PC.width // size_PC.height, 80))  # 进行缩放
                    im1 = im1.crop(((80 * size_PC.width // size_PC.height - 160) // 2, 0,
                                    (80 * size_PC.width // size_PC.height - 160) // 2 + 160, 80))  # 进行中心裁剪
            elif (size_mode == 2):  # 拉伸充满
                im1 = im.resize((size_USE_X1, size_USE_Y1))  # 进行缩放
            im2 = im1.load()  # 直接将内存的数组加载出来处理

            for y in range(0, size_USE_Y1):
                for x in range(0, size_USE_X1):
                    hex_16RGB.append(
                        ((im2[x, y][0] >> 3) << 11) | ((im2[x, y][1] >> 2) << 5) | (im2[x, y][2] >> 3))  # 先直接添加16bit数组
                    # hex_16RGB.append(Color_16bit>>8)
                    # hex_16RGB.append(Color_16bit%256)
                    # hex_16RGB.append((im2[x,y][0]>>3)*8+im2[x,y][1]//32)
                    # hex_16RGB.append(((im2[x,y][1]%32)//4)*32+im2[x,y][2]//8)

            if (G_screnn0_OK == 0):
                G_screnn0 = Screen_Date_Process(hex_16RGB)
                G_screnn0_OK = 1
            elif (G_screnn1_OK == 0):
                G_screnn1 = Screen_Date_Process(hex_16RGB)
                G_screnn1_OK = 1
            u_time1 = time.time() - u_time1
            print("截屏耗时" + str(u_time1))
        if MG_screen_thread_should_stop:
            print('stop screenshot')
            break # exit screenshot thread
        time.sleep(0.05)


def show_PC_Screen():  # 显示照片
    global State_change, Screen_Error, Device_State, Thread1
    global G_screnn0_OK, G_screnn1_OK, G_screnn0, G_screnn1, size_USE_X1, size_USE_Y1
    if (State_change == 1):
        State_change = 0
        Screen_Error = 0
        LCD_ADD((160 - size_USE_X1) // 2, (80 - size_USE_Y1) // 2, size_USE_X1, size_USE_Y1)
    if (State_change == 0):

        if (G_screnn0_OK == 1 or G_screnn1_OK == 1):
            # print("传输画面ing")
            u_time = time.time()
            if (G_screnn0_OK == 1):
                # LCD_ADD((240-size_USE_X1)//2,(240-size_USE_Y1)//2,size_USE_X1,size_USE_Y1)
                SER_Write(G_screnn0)
                G_screnn0_OK = 0
            elif (G_screnn1_OK == 1):
                # LCD_ADD((240-size_USE_X1)//2,(240-size_USE_Y1)//2,size_USE_X1,size_USE_Y1)
                SER_Write(G_screnn1)
                G_screnn1_OK = 0
            u_time = time.time() - u_time
            # print("传输耗时"+str(u_time))
            Screen_Error = 0
        else:
            Screen_Error = Screen_Error + 1
            if Screen_Error > 1000:
                Device_State = 0
                try:  # 尝试建立屏幕截屏线程
                    Thread1.stop()
                except:
                    print("警告,无法关闭截图线程")
                try:  # 尝试建立屏幕截屏线程
                    Thread1.start()
                except:
                    print("警告,无法创建截图线程")
            # print("无传输画面")
        time.sleep(0.05)


netspeed_last_refresh_time = None
netspeed_last_refresh_snetio = None
netspeed_plot_data = None

def sizeof_fmt(num, suffix="B", base=1024.0):
    # Use KiB for small value
    if abs(num) < base:
        return f"{num / base:3.1f}Ki{suffix}"
    for unit in ("", "Ki", "Mi", "Gi", "Ti", "Pi", "Ei", "Zi"):
        if abs(num) < base:
            return f"{num:3.1f}{unit}{suffix}"
        num /= base
    return f"{num:.1f}Yi{suffix}"

def show_netspeed(text_color=(255, 128, 0)):
    # geezmo: 预渲染图片，显示网速
    global netspeed_last_refresh_time, netspeed_last_refresh_snetio, netspeed_plot_data, State_change
    
    if netspeed_last_refresh_time is None or State_change == 1:
        # 初始化
        State_change = 0
        netspeed_last_refresh_time = datetime.now()
        netspeed_last_refresh_snetio = psutil.net_io_counters()
        netspeed_plot_data = [{'sent': 0, 'recv': 0}] * 120 # 存 120 格的绘图数据
        # 初始化的时候，网速先显示0
        current_snetio = netspeed_last_refresh_snetio
    else:
        current_snetio = psutil.net_io_counters()
    
    # 获取网速 bytes/second
    
    current_time = datetime.now()
    seconds_elapsed = (current_time - netspeed_last_refresh_time) / timedelta(seconds=1)
    
    sent_per_second = (current_snetio.bytes_sent - netspeed_last_refresh_snetio.bytes_sent) / seconds_elapsed
    recv_per_second = (current_snetio.bytes_recv - netspeed_last_refresh_snetio.bytes_recv) / seconds_elapsed
    # print(current_snetio.bytes_sent, netspeed_last_refresh_snetio.bytes_sent, seconds_elapsed)
    
    netspeed_plot_data = netspeed_plot_data[1:] + [{
        'sent': sent_per_second,
        'recv': recv_per_second,
    }]
    
    netspeed_last_refresh_time = current_time
    netspeed_last_refresh_snetio = current_snetio
    
    # 绘制图片
    
    im1 = Image.new('RGB', (size_USE_X1, size_USE_Y1), (0, 0, 0))
    # im1 = Image.open('示例.png')
    
    draw = ImageDraw.Draw(im1)
    font_size = 20
    try:
        font = ImageFont.truetype("simhei.ttf", font_size)
    except OSError:
        # Pillow 可能不能忽略文件大小写，以免读取失败
        font = ImageFont.truetype("SimHei.ttf", font_size)
    
    # 绘制文字
    
    text = f"上传  {sizeof_fmt(sent_per_second):>8}"
    draw.text((0, 0), text, fill=text_color, font=font)
    text = f"下载  {sizeof_fmt(recv_per_second):>8}"
    draw.text((0, 40), text, fill=text_color, font=font)

    # 绘图
    
    for start_y, key, color in zip([19, 59], ['sent', 'recv'], [(235, 139, 139), (146, 211, 217)]):
        sent_values = [data[key] for data in netspeed_plot_data]

        max_value = max(1024 * 100, max(sent_values)) # 最小范围 100KB/s
        bar_width = 2  # 每个点宽度
        image_height = 20  # 高度
        
        for i, sent in enumerate(sent_values[-80:]):
            # Scale the sent value to the image height
            bar_height = int((sent / max_value) * image_height) if max_value else 0
            x0 = i * bar_width
            y0 = image_height - bar_height
            x1 = (i + 1) * bar_width
            y1 = image_height

            # Draw the bar
            draw.rectangle([x0, start_y + y0, x1 - 1, max(start_y + y0, start_y + y1 - 1)], fill=color)  # Green bars

    
    im2 = im1.load()
    
    hex_16RGB = []  # bytearray()
    
    for y in range(0, size_USE_Y1):
        for x in range(0, size_USE_X1):
            hex_16RGB.append(
                ((im2[x, y][0] >> 3) << 11) | ((im2[x, y][1] >> 2) << 5) | (im2[x, y][2] >> 3))  # 先直接添加16bit数组
    hexstream = Screen_Date_Process(hex_16RGB)
    LCD_ADD((160 - size_USE_X1) // 2, (80 - size_USE_Y1) // 2, size_USE_X1, size_USE_Y1)
    SER_Write(hexstream)
    
    # 大约每1秒刷新一次
    time.sleep(1 - (datetime.now() - netspeed_last_refresh_time) / timedelta(seconds=1))


def UI_Page():  # 进行图像界面显示
    global Label1, root, s1, s2, s3, Label2, Label3, Label4, Label5, Label6, Text1
    # 创建主窗口
    root = tk.Tk()  # 实例化主窗口
    root.title("MG USB屏幕助手V1.0 20241202")  # 设置标题
    size_show = pyautogui.size()
    Show_X = int(size_show.width / 2) - int(Show_W / 2)
    Show_Y = int(size_show.height / 2) - int(Show_H / 2)
    root.geometry(str(Show_W) + "x" + str(Show_H) + "+" + str(Show_X) + "+" + str(Show_Y))  # 主窗口的大小以及在显示器上的位置
    # 创建按键
    btn1 = tk.Button(root, text="上翻页", height=1, width=8)
    btn1.place(x=400, y=275, anchor="w")  # 设置位置以及对齐方式
    btn1.config(command=Page_UP)  # 连接按键触发事件

    btn2 = tk.Button(root, text="下翻页", height=1, width=8)
    btn2.place(x=400, y=325, anchor="w")  # 设置位置以及对齐方式
    btn2.config(command=Page_Down)  # 连接按键触发事件

    btn3 = tk.Button(root, text="选择背景图像", height=1, width=12)
    btn3.place(x=250, y=75, anchor="w")  # 设置位置以及对齐方式
    btn3.config(command=Get_Photo_Path1)  # 连接按键触发事件

    btn4 = tk.Button(root, text="选择闪存固件", height=1, width=12)
    btn4.place(x=250, y=125, anchor="w")  # 设置位置以及对齐方式
    btn4.config(command=Get_Photo_Path2)  # 连接按键触发事件

    btn5 = tk.Button(root, text="烧写", height=1, width=8)
    btn5.place(x=400, y=75, anchor="w")  # 设置位置以及对齐方式
    btn5.config(command=Writet_Photo_Path1)  # 连接按键触发事件

    btn6 = tk.Button(root, text="烧写", height=1, width=8)
    btn6.place(x=400, y=125, anchor="w")  # 设置位置以及对齐方式
    btn6.config(command=Writet_Photo_Path2)  # 连接按键触发事件

    btn7 = tk.Button(root, text="切换显示方向", height=1, width=12)
    btn7.place(x=250, y=325, anchor="w")  # 设置位置以及对齐方式
    btn7.config(command=LCD_Change)  # 连接按键触发事件

    btn8 = tk.Button(root, text="烧写", height=1, width=8)
    btn8.place(x=400, y=175, anchor="w")  # 设置位置以及对齐方式
    btn8.config(command=Writet_Photo_Path3)  # 连接按键触发事件

    btn9 = tk.Button(root, text="烧写", height=1, width=8)
    btn9.place(x=400, y=225, anchor="w")  # 设置位置以及对齐方式
    btn9.config(command=Writet_Photo_Path4)  # 连接按键触发事件

    btn10 = tk.Button(root, text="选择相册图像", height=1, width=12)
    btn10.place(x=250, y=175, anchor="w")  # 设置位置以及对齐方式
    btn10.config(command=Get_Photo_Path3)  # 连接按键触发事件

    btn11 = tk.Button(root, text="选择动图文件", height=1, width=12)
    btn11.place(x=250, y=225, anchor="w")  # 设置位置以及对齐方式
    btn11.config(command=Get_Photo_Path4)  # 连接按键触发事件

    Label2 = None
    def update_label_color():
        global color_use
        color_La = '#{:02x}{:02x}{:02x}'.format(int(s1.get()) * 8, int(s2.get()) * 4, int(s3.get()) * 8)
        color_use = int(s1.get()) * 2048 + int(s2.get()) * 32 + int(s3.get())
        if Label2:
            Label2.config(bg=color_La)
    
    # 创建滑块
    s1 = Scale(root, from_=0, to=31, resolution=1, troughcolor='Red', orient=HORIZONTAL)  # orient=HORIZONTAL 横向，默认纵向
    s1.place(x=150, y=25, anchor="w")  # W.get()
    s1.set(31)
    s1.config(command=lambda x: update_label_color())
    s2 = Scale(root, from_=0, to=63, resolution=1, troughcolor='Green', orient=HORIZONTAL)  # orient=HORIZONTAL 横向，默认纵向
    s2.place(x=250, y=25, anchor="w")  # W.get()可获取滑块值
    s2.set(32)
    s2.config(command=lambda x: update_label_color())
    s3 = Scale(root, from_=0, to=31, resolution=1, troughcolor='Blue', orient=HORIZONTAL)  # orient=HORIZONTAL 横向，默认纵向
    s3.place(x=350, y=25, anchor="w")  # W.get()可获取滑块值
    s3.set(0)
    s3.config(command=lambda x: update_label_color())
    # 创建标签
    Label1 = tk.Label(root, text="设备未连接", bg="Red")
    Label1.place(x=25, y=25, anchor="w")  # 设置位置以及对齐方式
    
    Label2 = tk.Label(root, bg="#ff8000", width=2)
    Label2.place(x=460, y=25, anchor="w")  # 设置位置以及对齐方式
    
    update_label_color()
    
    Label3 = tk.Label(root, bg="white", width=21)
    Label3.place(x=5, y=75, anchor="w")  # 设置位置以及对齐方式

    Label4 = tk.Label(root, bg="white", width=21)
    Label4.place(x=5, y=125, anchor="w")  # 设置位置以及对齐方式

    Label5 = tk.Label(root, bg="white", width=21)
    Label5.place(x=5, y=175, anchor="w")  # 设置位置以及对齐方式

    Label6 = tk.Label(root, bg="white", width=21)
    Label6.place(x=5, y=225, anchor="w")  # 设置位置以及对齐方式

    # Text_Show=tk.StringVar()
    # Text_Show.set(0)
    # 创建文本框
    Text1 = tk.Text(root, width=23, height=4)
    Text1.place(x=5, y=300, anchor="w")  # 设置位置以及对齐方式
    # Text1.delete(0,END)
    # Text1.insert(END,'内容一')#在文本框开始位置插入“内容一”
    Text1.delete(1.0, END)  # 清除文本框

    # 进入消息循环

    root.mainloop()
    


def Get_MSN_Device():  # 尝试获取MSN设备
    global Device_State, ADC_det, Thread1, ser, State_change, State_machine, My_MSN_Device, My_MSN_Data, Screen_Error, Label1, LCD_Change_now, Text1
    port_list = list(serial.tools.list_ports.comports())  # 查询所有串口
    if len(port_list) == 0:
        print('未检测到串口,请确保设备已连接到电脑')
        # Label1.config(text="设备已连接",bg="GREEN")
        time.sleep(1)
        Label1.config(text="设备未连接", bg="RED")
        Device_State = 0  # 未能连接
        try:  # 尝试建立屏幕截屏线程
            Thread1.stop()
        except:
            print("警告,无法关闭截图线程")
    else:  # 对串口进行监听，确保其为MSN设备
        My_MSN_Device = []
        My_MSN_Data = []
        for i in range(0, len(port_list)):
            try:  # 尝试打开串口
                ser = serial.Serial(port_list[i].name, 19200, timeout=2)  # 初始化串口连接,初始使用
            except:  # 出现异常
                print(port_list[i].name + '无法打开,请检查是否被其他程序占用')  # 显示MSN设备数量
                # ser.close()#将串口关闭，防止下次无法打开
                time.sleep(0.1)
                continue  # 执行下一次循环
            time.sleep(0.25)  # 理论上MSN设备100ms要发送一次“ MSN01”,在250ms内至少会收到一次
            recv = SER_Read()
            if (recv == 0):
                break  # 退出当前for循环
            else:
                recv = recv.decode("gbk")  # 获取串口数据
            if (len(recv) > 5):  # 收到6个字符以上数据时才进行解析
                for n in range(0, len(recv) - 5):  # 逐字解析编码
                    if (ord(recv[n]) == 0):  # 当前字节为0时进行解析
                        if ((recv[n + 1] == 'M') and (recv[n + 2] == 'S') and (recv[n + 3] == 'N')):  # 确保为MSN设备
                            if ((recv[n + 4] >= '0') and (recv[n + 4] <= '9') and (recv[n + 5] >= '0') and (
                                    recv[n + 5] <= '9')):  # 确保版本号为数字ASC码
                                My_MSN_Device.append(MSN_Device((port_list[i].name), (ord(recv[4]) - 48) * 10 + (
                                            ord(recv[5]) - 48)))  # 对MSN设备进行登记
                                hex_code = int(0).to_bytes(1, byteorder="little")  # 可以逐个加入数组
                                hex_code = hex_code + b'MSNCN'
                                SER_Write(hex_code)  # 返回消息
                                # 等待返回消息，确认连接
                                time.sleep(0.25)  # 理论上MSN设备100ms要发送一次“ MSN01”,在250ms内至少会收到一次
                                recv = SER_Read().decode("gbk")  # 获取串口数据
                                if ((ord(recv[0]) == 0) and (recv[1] == 'M') and (recv[2] == 'S') and (
                                        recv[3] == 'N') and (recv[4] == 'C') and (recv[5] == 'N')):  # 确保为MSN设备
                                    print('MSN设备' + str(len(My_MSN_Device)) + '——' + port_list[
                                        i].name + '连接完成')  # 显示MSN设备数量
                                else:
                                    print('MSN设备' + str(
                                        len(My_MSN_Device)) + '无法连接,请检查连接是否正常')  # 显示MSN设备数量
                                break  # 退出当前for循环
        print('MSN设备数量为' + str(len(My_MSN_Device)) + '个')  # 显示MSN设备数量
        if (len(My_MSN_Device) >= 1 and Device_State == 0):
            Device_State = 1  # 可以正常连接
            State_change = 1  # 状态发生变化
            Screen_Error = 0
            Read_M_SFR_Data(256)  # 读取u8在0x0100之后的128字节
            Print_MSN_Data()  # 解析字节中的数据格式
            Read_MSN_Data(b'MSN_Status')
            UID = Read_MSN_Data(b'MSN_UID')
            try:  # 尝试建立屏幕截屏线程
                Thread1.start()
            except:
                try:  # 尝试建立屏幕截屏线程
                    Thread1.stop()
                except:
                    print("警告,无法关闭截图线程")
                print("警告,无法创建截图线程")
            # 获取按键状态
            # LCD_State(1)#配置显示方向
            ADC_det = Read_ADC_CH(9)
            ADC_det = (ADC_det + Read_ADC_CH(9)) / 2
            ADC_det = ADC_det - 125  # 根据125的阈值判断是否被按下
            Label1.config(text="设备已连接", bg="GREEN")
            LCD_Change_now = 0
            Text1.delete(1.0, END)  # 清除文本框
            # Text1.insert(END,'设备识别码:')#在文本框开始位置插入“内容一”

            # Label1=tk.Label(root,text="设备已连接",bg="GREEN")

            # for i in range(1,37):
            #   Write_Flash_Photo_fast(100*(i-1),str(i))#160*80分辨率彩色图片，占用100个Page

            # Write_Flash_Photo_fast(3600,'Demo1')#240*240单色图片，占用29个Page
            # Write_Flash_Photo_fast(3629,'N48X66P')#48*66分辨率数码管图像，占用22个Page

            # Write_Flash_ZK(3651,'ASC64')#32*64分辨率ASCII表格，占用128个Page

            # Write_Flash_Photo_fast(3779,'logo')#240*102单色LOGO,占用12个Page
            # Write_Flash_Photo_fast(3791,'J1')#240*240单色图片，占用29个Page

            # Write_Flash_Photo_fast(3820,'MLOGO')#160*68单色图片，占用6个Page
            # Write_Flash_Photo_fast(3826,'CLK_BG')#160*80彩色图片，占用100个Page
            # Write_Flash_Photo_fast(3926,'PH1')#160*80彩色图片，占用100个Page
            # Write_Flash_Photo_fast(4026,'N24X33P')#24*33分辨率数码管图像，占用12个Page
            # Write_Flash_Photo_fast(4038,'MP1')#160*80单色图片，占用7个Page

        else:
            Device_State = 0  # 可以正常连接
            # try:#尝试关闭屏幕截屏线程
            #    Thread1.stop()
            # except:
            #    print("警告,无法关闭截图线程")


def MSN_Device_1_State_machine():  # MSN设备1的循环状态机
    global State_machine, time_out, key_eff, key_on, State_change, s1, s2, s3, color_use, Label2, photo_path1, photo_path2, write_path1, write_path2, LCD_Change_use, LCD_Change_now, write_path3, write_path4, Img_data_use
    # print("State_machine"+str(State_machine))
    # if write_path1==1:
    if LCD_Change_now != LCD_Change_use:  # 显示方向与设置不符合
        LCD_Change_now = LCD_Change_use
        LCD_State(LCD_Change_now)  # 配置显示方向
        State_change = 1

    if (write_path1 == 1):
        Write_Flash_hex_fast(3826, Img_data_use)
        write_path1 = 0
        State_change = 1
    if (write_path2 == 1):
        Write_Flash_Photo_fast(0, photo_path2)
        write_path2 = 0
        State_change = 1

    if (write_path3 == 1):
        Write_Flash_hex_fast(3926, Img_data_use)
        write_path3 = 0
        State_change = 1

    if (write_path4 == 1):
        Write_Flash_hex_fast(0, Img_data_use)
        write_path4 = 0
        State_change = 1

    if (time_out == 1):
        time_out = 0
        if (Read_ADC_CH(9) < ADC_det):  # 按键按下
            key_on = 1
        elif (key_on == 1):
            key_eff = 1
            key_on = 0
        else:
            key_on = 0
        if (key_eff == 1):
            key_eff = 0
            State_machine = State_machine + 1
            if (State_machine > 5):
                State_machine = 0
            State_change = 1
    elif (State_machine == 0):
        show_gif()
    elif (State_machine == 1):
        show_PC_state(BLUE, BLACK)
    elif (State_machine == 2):
        show_PC_state(color_use, BLACK)
    elif (State_machine == 3):
        show_Photo1()
    elif (State_machine == 4):
        show_PC_time()
    elif (State_machine == 5):
        show_PC_Screen()
    elif (State_machine == 3901):
        rgb_tuple = (((color_use >> 11) & 0x1F) * 255 // 31, ((color_use >> 5) & 0x3F) * 255 // 63, (color_use & 0x1F) * 255 // 31)
        show_netspeed(text_color=rgb_tuple)


print("该设备具有" + str(psutil.cpu_count(logical=False)) + "个内核和" + str(psutil.cpu_count()) + "个逻辑处理器")
print("该CPU主频为" + str(round((psutil.cpu_freq().current / 1000), 1)) + "GHZ")
print("当前CPU占用率为" + str(psutil.cpu_percent()) + "%")  # 并不准确
mem = psutil.virtual_memory()
print("该设备具有" + str(round(mem.total / (1024 * 1024 * 1024))) + "GB的内存")
print("当前内存占用率为" + str(mem.percent) + "%")
print("开始运行时间" + datetime.fromtimestamp(psutil.boot_time()).strftime("%Y-%m-%d %H:%M:%S"))
battery = psutil.sensors_battery()

if battery != None:
    print("电池剩余电量" + str(battery.percent) + "%")
# if battery.power_plugged:
#	print("已连接电源线")
# else:
#	print("已断开电源线")

# 创建定时器,延时为0.2秒
D = 0
# while(1):
#    D=D+1
#    print(D)
#    time.sleep(0.5)


time_out = 0
CPU = 0
FC = BLUE
BC = BLACK
key_on = 0
key_eff = 0
State_change = 1  # 状态发生变化
gif_num = 0
# State_machine = 3901  # 定义初始状态
State_machine = 2 # 定义初始状态
Device_State = 0  # 初始为未连接
LCD_Change_use = 0  # 初始显示方向
LCD_Change_now = 0
color_use = RED
write_path1 = 0
write_path2 = 0
write_path3 = 0
write_path4 = 0
photo_path1 = ""
photo_path2 = ""
photo_path3 = ""
photo_path4 = ""

Thread1 = threading.Thread(target=Screen_Date_get) 

def daemon_task():
    global D
        
    while (1):
        D = D + 1
        # print(D)
        if (Device_State == 0):  # 未检测到设备
            Get_MSN_Device()  # 尝试获取MSN设备
        # print("Waiting")
        elif (Device_State == 1):  # 已检测到设备
            MSN_Device_1_State_machine()
        if MG_daemon_should_stop:
            print('stop daemon')
            break
            # time.sleep(10)
        # print("OK")

daemon_thread = threading.Thread(target=daemon_task)
daemon_thread.start()

# tkinter requires the main thread
try:
    UI_Page()
finally:
    # reap threads
    print('ui closed')

    MG_screen_thread_should_stop = True
    MG_daemon_should_stop = True
    print('reap threads')
    Thread1.join()
    daemon_thread.join()

