#!/usr/bin/env python
import os
import time
import requests

SNums = [0, 1, 2, 3]  # Numbers of the Servos we'll be using in ServoBlaster
SName = ["Waist", "Left", "Right", "Claw"]  # Names of Servos
AMins = [0, 0, 0, 0]  # Minimum angles for Servos 0-3
AMaxs = [180, 180, 180, 180]  # Maximum angles for Servos 0-3
ACurs = [90, 0, 0, 60]  # Current angles being set as the intial angles


os.system(
    'sudo /home/pi/PiBits/ServoBlaster/user/servod --idle-timeout=2000')  # This line is sent to command line to start the servo controller


def GoDirectlyTo(Servo, Pos):
    ACurs[Servo] = Pos
    micro = (1000 + (ACurs[Servo] * 8.3333))
    print(ACurs[Servo], micro)
    os.system("echo %d=%dus > /dev/servoblaster" % (SNums[Servo], micro))


def StepGoTo(Servo, Pos, Step):
    if ACurs[Servo] > Pos:
        Step = 0 - Step

    while ACurs[Servo] < AMaxs[Servo] & ACurs[Servo] > AMins[Servo]:
        GoDirectlyTo(Servo, ACurs[Servo] + Step)

    GoDirectlyTo(Servo, Pos)


def Reset():
    GoDirectlyTo(3, 60)
    StepGoTo(0, 90, 5)


def Feed():
    StepGoTo(0, 120, 5)
    GoDirectlyTo(3, 90)
    GoDirectlyTo(3, 60)
    time.sleep(1)
    GoDirectlyTo(3, 90)

    Reset()


def TakePics():
    for i in range(5):
        os.system("raspistill -o %s_%s.jpg" % (time.strftime("%Y%m%d"), i))
        if i < 3:
            time.sleep(10)
        else:
            time.sleep(60)


while 1 == 1:
    try:
        url = 'http://xfeed.azurewebsites.net/api/oktofeed'
        response = requests.get(url)
        if response == "true":
            Feed()
            url = 'http://xfeed.azurewebsites.net/api/feeddone'
        requests.get(url)

        time.sleep(5)
        TakePics()

        url = 'http://xfeed.azurewebsites.net/api/uploadimages'
        for i in range(5):
            path = "%s/%s_%s.jpg" % (time.strftime("%Y%m%d"), time.strftime("%Y%m%d"), i)
            files = {'file': (FILE, open(path, 'rb'), 'image/jpg', {'Expires': '0'})}
            r = requests.post(url, files=files)

    except Exception as e:
        # TODO log
        print('ERROR:', e)

    time.sleep(5)
