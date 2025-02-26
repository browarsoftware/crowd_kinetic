import csv
import math

import numpy as np
from numba import jit

def read_red(file_path, split_string = ',', skip_header = True):
    all_data = []
    with open(file_path, newline='') as csvfile:
        spamreader = csv.reader(csvfile, delimiter=',', quotechar='|')
        b = 0
        for row in spamreader:
            if skip_header and b == 0:
               foo = 0
               b = b + 1
            else:
                if len(row) == 1:
                    xxx = row[0].split(split_string)
                else:
                    xxx = row
                xxx_ = np.zeros(len(xxx))
                for a in range(len(xxx)):
                    xxx_[a] = float(xxx[a])
                all_data.append(xxx_)
    return np.array(all_data)

@jit(nopython=True)
def interpolate(x1: float, x2: float, y1: float, y2: float, x: float):
    """Perform linear interpolation for x between (x1,y1) and (x2,y2) """
    return ((y2 - y1) * x + x2 * y1 - x1 * y2) / (x2 - x1)

@jit(nopython=True)
def calculate_res(my_array, all_data):
    for a in range(len(all_data)):
        x_inter = round(interpolate(-6, 6, 0, my_array.shape[0], all_data[a][2] / 100))
        y_inter = round(interpolate(-6, 6, 0, my_array.shape[1], all_data[a][3] / 100))
        my_array[x_inter, y_inter] = my_array[x_inter, y_inter] + 1
    return my_array

@jit(nopython=True)
def calculate(my_array, all_data):
    for a in range(len(all_data)):
        x_inter = round(interpolate(-6, 6, 0, my_array.shape[0], all_data[a][3]))
        y_inter = round(interpolate(-6, 6, 0, my_array.shape[1], all_data[a][5]))
        my_array[x_inter, y_inter] = my_array[x_inter, y_inter] + 1
    return my_array

import cv2
from scipy.ndimage import gaussian_filter

def read_data_ref(file_path, split_string, skip_header,my_size):
    res = read_red(file_path, split_string, skip_header)
    my_array = np.zeros((my_size, my_size))

    my_array = calculate_res(my_array, res)

    my_array = gaussian_filter(my_array, sigma=1)

    my_array = my_array / np.max(my_array)
    my_array_img = (my_array * 255).astype('uint8')
    return [my_array, my_array_img]

def read_data(file_path, skip_header, split_string, my_size):
    res = read_red(file_path, skip_header, split_string)
    my_array = np.zeros((my_size, my_size))

    my_array = calculate(my_array, res)

    my_array = gaussian_filter(my_array, sigma=1)

    my_array = my_array / np.max(my_array)
    my_array_img = (my_array * 255).astype('uint8')
    return [my_array, my_array_img]

def make_color(my_array_img_res):
    imC = cv2.applyColorMap(my_array_img_res, cv2.COLORMAP_JET)
    imC = cv2.resize(imC, (256, 256), interpolation=cv2.INTER_NEAREST)
    return imC


# 0.013963375908614989
#[my_array, my_array_img] = read_data('sim90_1500_1_100_1_100.csv', ',', True, szie_help)
# 0.0125750013627293
#[my_array, my_array_img] = read_data('sim90_2100_2_200_2_200.csv', ',', True, szie_help)

from sklearn.metrics import mean_squared_error

szie_help = 31
"""
[my_array_res, my_array_img_res] = read_data_ref('b090.txt', ' ', False, szie_help)
cv2.imwrite("results/90/b090.png", make_color(my_array_img_res))

force = [1500, 1700, 1900, 2100]
d_ = [1, 2]
f_ = [100, 200]
d_w = [1, 2]
f_w = [100, 200]
# 1700_2_100_1_200.csv
list_results = []
file_path = 'd:/data/crowd_forces/90/sim90_'
for force_id in range(len(force)):
    for d_id in range(len(d_)):
        for f_id in range(len(f_)):
            for d_w_id in range(len(d_w)):
                for f_w_id in range(len(f_w)):
                    file_path_help = str(force[force_id]) + "_" + \
                                     str(d_[d_id]) + "_" + str(f_[f_id]) + "_" \
                                   + str(d_w[d_w_id]) + "_" + str(f_w[f_w_id])
                    my_file_path = file_path + file_path_help + ".csv"
                    [my_array, my_array_img] = read_data(my_file_path, ',', True, szie_help)
                    cv2.imwrite("results/90/" + file_path_help + ".png", make_color(my_array_img))
                    smse = math.sqrt(mean_squared_error(my_array_res, my_array))
                    list_results.append(smse)
                    print(file_path_help + ": " + str(smse))
print(min(list_results))
"""

my_file_path_red_help = 'd:/data/crowd_forces/ref/'

my_file_path_red = ['b090.txt',
                    'b100.txt',
                    'b110.txt',
                    'b120.txt',
                    'b140.txt',
                    'b160.txt',
                    'b180.txt',
                    'b200.txt',
                    'b220.txt',
                    'b250.txt']

my_file_path_help = "d:/data/crowd_forces/"

my_file_path = ['sim90_2025-2-25-0-58-12.csv',
                'sim100_2025-2-25-0-55-0.csv',
                'sim110_2025-2-25-0-52-8.csv',
                'sim120_2025-2-25-0-48-35.csv',
                'sim140_2025-2-25-0-44-46.csv',
                'sim160_2025-2-25-0-41-34.csv',
                'sim180_2025-2-25-0-38-16.csv',
                'sim200_2025-2-25-0-34-6.csv',
                'sim220_2025-2-25-0-31-2.csv',
                'sim250_2025-2-25-0-29-0.csv'
                ]
"""
my_file_path = ['sim100_2025-2-23-18-59-38.csv',
                'sim110_2025-2-23-19-12-3.csv',
                'sim120_2025-2-23-19-16-19.csv',
                'sim140_2025-2-23-19-26-4.csv',
                'sim160_2025-2-23-21-1-34.csv',
                'sim180_2025-2-23-21-4-7.csv',
                'sim200_2025-2-23-21-6-37.csv',
                'sim220_2025-2-23-21-9-9.csv',
                'sim250_2025-2-23-21-13-11.csv']
"""

for a in range(len(my_file_path)):
    [my_array_res, my_array_img_res] = read_data_ref(my_file_path_red_help + my_file_path_red[a], ' ', False, szie_help)
    cv2.imwrite("results/" + my_file_path_red[a] + ".png", make_color(my_array_img_res))
    [my_array, my_array_img] = read_data(my_file_path_help + my_file_path[a], ',', True, szie_help)
    cv2.imwrite("results/" + my_file_path[a] + ".png", make_color(my_array_img))
    smse = math.sqrt(mean_squared_error(my_array_res, my_array))
    print(my_file_path[a] + ": " + str(smse))


"""
cv2.imshow("my_array_ref", my_array_res)
imC = cv2.applyColorMap(my_array_img_res, cv2.COLORMAP_JET)
imC = cv2.resize(imC, (256, 256), interpolation = cv2.INTER_NEAREST)
cv2.imshow("my_array_img_ref", imC)


cv2.imshow("my_array", my_array)
imC = cv2.applyColorMap(my_array_img, cv2.COLORMAP_JET)
imC = cv2.resize(imC, (256, 256), interpolation = cv2.INTER_NEAREST)
cv2.imshow("my_array_img", imC)

from sklearn.metrics import mean_squared_error

sum = 0
for x in range(my_array_res.shape[0]):
    for y in range(my_array_res.shape[1]):
        sum = sum + (my_array_res[x,y] - my_array[x,y]) * (my_array_res[x,y] - my_array[x,y])
sum = sum / (my_array_res.shape[0] * my_array_res.shape[1])

mse = mean_squared_error(my_array_res,my_array)
print(sum)
print(mse)
cv2.waitKey(0)
"""