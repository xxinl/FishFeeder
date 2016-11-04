#!/usr/bin/env python
import os
import time
import requests
import sys
import glob


servo_name = ["Waist", "Left", "Right", "Claw"]  # Names of Servos
servo_min_pos = [0, 0, 0, 0]  # Minimum angles for Servos 0-3
servo_max_pos = [180, 180, 180, 180]  # Maximum angles for Servos 0-3
servo_init_pos = [90, 0, 0, 60]
servo_curr_pos = servo_init_pos  # Current angles being set as the initial angles
servo_move_step = [5, 5, 5, 0]

WIN_DEBUG = 1
base_url = 'http://localhost:20079/api/'

feed_steps = [
    servo_init_pos,
    [120, 0, 0, 60],
    [120, 0, 0, 90],
    [120, 0, 0, 60],
    [-1, -1, -1, -1],
    [120, 0, 0, 90],
    servo_init_pos]


def go_directly_to(servo_idx, pos):
    micro = (1000 + (pos * 8.3333))
    print(servo_name[servo_idx], 'moving to pos', pos, micro)
    if WIN_DEBUG != 1:
        os.system("echo %d=%dus > /dev/servoblaster" % (servo_idx, micro))
    servo_init_pos[servo_idx] = pos


def step_go_to(servo_idx, pos, step):
    if servo_curr_pos[servo_idx] > pos:
        step = 0 - step

    # TODO pos out of min max range, currently assume all pos inputs are valid within min max range
    while servo_init_pos[servo_idx] < servo_max_pos[servo_idx] & servo_init_pos[servo_idx] > servo_min_pos[servo_idx]:
        go_directly_to(servo_idx, servo_curr_pos[servo_idx] + step)

    go_directly_to(servo_idx, pos)


def arm_go_to_pos(pos):
    for idx in range(4):
        if servo_move_step[idx] == 0:
            go_directly_to(idx, pos[idx])
        else:
            step_go_to(idx, pos[idx], servo_move_step[idx])


def reset():
    arm_go_to_pos(servo_init_pos)


def feed():
    for idx in range(len(feed_steps)):
        if feed_steps[idx][0] == -1:
            time.sleep(1)
        else:
            arm_go_to_pos(feed_steps[idx])


def take_pics():
    for idx in range(5):
        if WIN_DEBUG != 1:
            os.system("raspistill -o %s_%s.jpg -w 1024 -h 768" % (time.strftime("%Y%m%d_%H%M%S"), idx))
            if idx < 3:
                time.sleep(10)
            else:
                time.sleep(60)


if WIN_DEBUG != 1:
    # This line is sent to command line to start the servo controller
    os.system('sudo /home/pi/PiBits/ServoBlaster/user/servod --idle-timeout=2000')


while 1 == 1:
    is_cam_streaming = False
    try:
        #camera streaming
        url = base_url + 'startstream'
        response = requests.get(url)
        is_cam_streaming = response.content == b'true'
        if is_cam_streaming:
            if WIN_DEBUG != 1:
                os.system("raspistill -o stream.jpg -w 800 -h 600")
            url = base_url + 'streamup'
            files = [('image', ('stream.jpg', open('stream.jpg', 'rb'), 'image/jpg', {'Expires': '0'}))]
            r = requests.post(url, files=files)

        #feeding
        url = base_url + 'oktofeed'
        response = requests.get(url)
        feeding = response.content
        if feeding == b'true':
            print('feeding', time.strftime("%Y%m%d_%H%M%S"))
            feed()
            url = base_url + 'feeddone'
            requests.get(url)

            take_pics()

            url = base_url + 'uploadimages'
            images = glob.glob('*.jpg')
            if len(images) > 0:
                print('uploading images', len(images))
                files = [('image', (path, open(path, 'rb'), 'image/jpg', {'Expires': '0'})) for path in images]
                r = requests.post(url, files=files)

                [f[1][1].close() for f in files]

                print('deleting images', len(images))
                [os.remove(path) for path in images]

            print('feed done', time.strftime("%Y%m%d_%H%M%S"))

    except Exception as e:
        msg = str(e)
        print('ERROR:', msg)

        try:
            url = base_url + 'logerror?msg=' + msg
            requests.get(url)
        except Exception as e:
            print('ERROR:', str(e))

    if not is_cam_streaming:
        if WIN_DEBUG != 1:
            time.sleep(15)
        else:
            time.sleep(5)
