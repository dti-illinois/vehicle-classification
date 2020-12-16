import cv2
import os
import sys


def get_metadata(metadata_file):
    metadata = []
    image_id = metadata_file.split('.')[-2].split('/')[2]
    with open(metadata_file, 'r') as mf:
        mf.readline()
        line = mf.readline()[:-1]
        while line:
            fields = line.split(',')
            frame_pos = (int(fields[0].split()[0]), int(fields[0].split()[1]))
            frame_size = (int(fields[1].split()[0]), int(fields[1].split()[1]))
            vehicle_name = fields[2]
            vehicle_id = fields[3]
            metadata.append((image_id, vehicle_name, vehicle_id,
                             frame_pos, frame_size))
            line = mf.readline()[:-1]
        mf.close()
    return metadata


def get_sub_image_coordinates(x, y, w, h):
    game_x_offset = 121
    game_y_offset = 29

    game_h_scale = 1.46
    game_w_scale = 1.315

    x_1 = int(game_x_offset + (x * game_w_scale) - (0.5 * w * game_w_scale))
    y_1 = int(game_y_offset + (y * game_h_scale) - (0.5 * h * game_h_scale))
    x_2 = x_1 + int(w * game_w_scale)
    y_2 = y_1 + int(h * game_h_scale)

    return (x_1, y_1, x_2, y_2)


def process_image(image_file, use_id):
    metadata_file = image_file[:-4] + '.csv'
    metadata = get_metadata(metadata_file)
    img = cv2.imread(image_file)
    for vehicle_metadata in metadata:
        x, y = vehicle_metadata[3]
        w, h = vehicle_metadata[4]

        x_1, y_1, x_2, y_2 = get_sub_image_coordinates(x, y, w, h)
        sub_img = img[y_1:y_2, x_1:x_2]

        image_id = vehicle_metadata[0]
        vehicle_name = vehicle_metadata[1]
        vehicle_id = vehicle_metadata[2]
        try:
            if use_id:
                os.mkdir(f'../processed_images/{vehicle_id}')
            else:
                os.mkdir(f'../processed_images/{vehicle_name}')
        except Exception:
            pass
        if use_id:
            filename = f'../processed_images/{vehicle_id}/{image_id}.png'
        else:
            filename = f'../processed_images/{vehicle_name}/{image_id}.png'
        cv2.imwrite(filename, sub_img)


def process_images(dir, use_id=True):
    files = os.listdir(dir)
    image_files = set()
    for file in files:
        image_files.add(file.split('.')[0] + '.png')
    image_files = list(image_files)
    for image_file in image_files:
        process_image(dir + '/' + image_file, use_id)


if __name__ == '__main__':
    if len(sys.argv) == 2:
        process_images('../images', sys.argv[1] == 'id')
    else:
        process_images('../images')
