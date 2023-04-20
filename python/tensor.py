import tensorflow as tf
import tensorflow_datasets as tfds

ds = tfds.load('wmt19_translate')
assert isinstance(ds, tf.data.Dataset)
print(ds)